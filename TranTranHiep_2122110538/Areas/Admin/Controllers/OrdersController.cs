using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.Services;

namespace TranTranHiep_2122110538.Areas.Admin.Controllers;

[Area("Admin")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("[area]/[controller]/[action]/{id?}")]
public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOrderAuditService _audit;
    private readonly IOrderNotificationService _notify;
    private readonly IInventoryService _inventory;

    public OrdersController(
        AppDbContext db,
        IOrderAuditService audit,
        IOrderNotificationService notify,
        IInventoryService inventory)
    {
        _db = db;
        _audit = audit;
        _notify = notify;
        _inventory = inventory;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 15, string? status = null, int? restaurantId = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);

        var query = _db.Orders.AsNoTracking().Include(o => o.User).Include(o => o.Restaurant).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);
        if (restaurantId.HasValue)
            query = query.Where(o => o.RestaurantId == restaurantId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.UserId,
                Username = o.User!.Username,
                o.RestaurantId,
                RestaurantName = o.Restaurant!.Name,
                o.OrderDate,
                o.TotalAmount,
                o.Status,
                o.Address,
                o.Phone,
                o.PaymentMethod,
                o.PaymentStatus,
                o.PaidAt,
                o.TrackingNumber,
                o.RefundedAt
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    /// <summary>Xuất toàn bộ đơn (lọc tuỳ chọn) ra CSV — mở bằng Excel.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportCsv(string? status = null, int? restaurantId = null)
    {
        var query = _db.Orders.AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.Restaurant)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);
        if (restaurantId.HasValue)
            query = query.Where(o => o.RestaurantId == restaurantId.Value);

        var rows = await query
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new
            {
                o.Id,
                o.UserId,
                Username = o.User!.Username,
                o.RestaurantId,
                RestaurantName = o.Restaurant!.Name,
                o.OrderDate,
                o.TotalAmount,
                o.Status,
                o.PaymentMethod,
                o.PaymentStatus,
                o.Address,
                o.Phone
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Id,UserId,Username,RestaurantId,RestaurantName,OrderDate,TotalAmount,Status,PaymentMethod,PaymentStatus,Address,Phone");
        foreach (var o in rows)
        {
            sb.Append(EscapeCsv(o.Id)).Append(',')
                .Append(EscapeCsv(o.UserId)).Append(',')
                .Append(EscapeCsv(o.Username)).Append(',')
                .Append(EscapeCsv(o.RestaurantId)).Append(',')
                .Append(EscapeCsv(o.RestaurantName)).Append(',')
                .Append(EscapeCsv(o.OrderDate.ToString("o", CultureInfo.InvariantCulture))).Append(',')
                .Append(EscapeCsv(o.TotalAmount.ToString(CultureInfo.InvariantCulture))).Append(',')
                .Append(EscapeCsv(o.Status)).Append(',')
                .Append(EscapeCsv(o.PaymentMethod)).Append(',')
                .Append(EscapeCsv(o.PaymentStatus)).Append(',')
                .Append(EscapeCsv(o.Address)).Append(',')
                .Append(EscapeCsv(o.Phone))
                .AppendLine();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"orders_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    private static string EscapeCsv(object? value)
    {
        var s = value?.ToString() ?? "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\r') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest body)
    {
        if (!OrderStatuses.All.Contains(body.Status))
            return BadRequest(new { message = "Trạng thái không hợp lệ." });

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return NotFound();

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var from = order.Status;
        order.Status = body.Status;
        if (body.Status == OrderStatuses.Cancelled)
        {
            order.CancelledAt = DateTime.UtcNow;
            order.CancelledBy = OrderCancelledBy.Admin;
        }

        _audit.AddStatusChange(order.Id, from, body.Status, adminId, Roles.Admin, null);

        await _db.SaveChangesAsync();

        if (InventoryRules.ShouldRestoreStockOnCancel(from, body.Status))
            await _inventory.RestoreStockForOrderAsync(order.Id);

        await _notify.BroadcastOrderAsync(order);
        _notify.LogEmailStub($"Admin cập nhật đơn #{order.Id}", $"{from} → {body.Status}");

        return Ok(new { message = "Đã cập nhật trạng thái.", order.Id, order.Status });
    }

    public class RefundRequest
    {
        public string Reason { get; set; } = string.Empty;
        /// <summary>Bỏ trống = hoàn toàn bộ tổng đơn.</summary>
        public decimal? Amount { get; set; }
    }

    /// <summary>Hoàn tiền sau khi đơn hoàn thành và đã thanh toán (ghi OrderPayments + audit).</summary>
    [HttpPost]
    public async Task<IActionResult> Refund(int id, [FromBody] RefundRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.Reason))
            return BadRequest(new { message = "Cần lý do hoàn tiền." });

        var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return NotFound();

        if (order.PaymentStatus != PaymentStatuses.Paid)
            return BadRequest(new { message = "Chỉ hoàn khi đơn đã thanh toán." });

        if (order.Status != OrderStatuses.Completed)
            return BadRequest(new { message = "Chỉ hoàn tiền khi đơn đã hoàn thành (nghiệp vụ demo)." });

        var refundAmount = body.Amount ?? order.TotalAmount;
        if (refundAmount <= 0 || refundAmount > order.TotalAmount)
            return BadRequest(new { message = "Số tiền hoàn không hợp lệ." });

        order.PaymentStatus = PaymentStatuses.Refunded;
        order.RefundedAt = DateTime.UtcNow;
        var note = body.Reason.Trim();
        order.RefundReason = note.Length > 500 ? note[..500] : note;

        _db.OrderPayments.Add(new OrderPayment
        {
            OrderId = order.Id,
            Amount = refundAmount,
            Kind = PaymentKinds.Refund,
            Method = order.PaymentMethod,
            Status = PaymentStatuses.Refunded,
            Note = order.RefundReason
        });

        _audit.AddStatusChange(
            order.Id,
            OrderStatuses.Completed,
            OrderStatuses.Completed,
            adminId,
            Roles.Admin,
            $"Hoàn tiền {refundAmount:N0}: {order.RefundReason}");

        await _db.SaveChangesAsync();

        await _notify.BroadcastOrderAsync(order);
        _notify.LogEmailStub($"Hoàn tiền đơn #{order.Id}", $"{refundAmount} — {order.RefundReason}");

        return Ok(new { message = "Đã ghi nhận hoàn tiền.", order.Id, refundAmount, order.PaymentStatus });
    }
}
