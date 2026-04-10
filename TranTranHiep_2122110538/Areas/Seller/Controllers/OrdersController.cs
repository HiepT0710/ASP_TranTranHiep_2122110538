using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.Services;

namespace TranTranHiep_2122110538.Areas.Seller.Controllers;

[Area("Seller")]
[ApiController]
[Authorize(Roles = Roles.Seller)]
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

    private async Task<Restaurant?> GetMyRestaurantAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _db.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == userId);
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 15, string? status = null)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);

        var query = _db.Orders.AsNoTracking().Include(o => o.User)
            .Where(o => o.RestaurantId == rest.Id);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

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
                o.OrderDate,
                o.TotalAmount,
                o.Status,
                o.Address,
                o.Phone,
                o.PaymentMethod,
                o.PaymentStatus,
                o.TrackingNumber,
                o.ShipperName
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, restaurantId = rest.Id, items });
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public string? ShipperName { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest body)
    {
        if (!OrderStatuses.SellerAssignable.Contains(body.Status))
            return BadRequest(new { message = "Seller chỉ được đặt: Preparing, Delivering, Completed." });

        var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.RestaurantId == rest.Id);
        if (order == null)
            return NotFound();

        var from = order.Status;
        order.Status = body.Status;

        if (body.Status == OrderStatuses.Delivering || body.Status == OrderStatuses.Completed)
        {
            if (!string.IsNullOrWhiteSpace(body.TrackingNumber))
            {
                var t = body.TrackingNumber.Trim();
                order.TrackingNumber = t.Length > 100 ? t[..100] : t;
            }

            if (!string.IsNullOrWhiteSpace(body.ShipperName))
            {
                var s = body.ShipperName.Trim();
                order.ShipperName = s.Length > 200 ? s[..200] : s;
            }
        }

        if (body.Status == OrderStatuses.Completed
            && order.PaymentMethod == PaymentMethods.COD
            && order.PaymentStatus == PaymentStatuses.Pending)
        {
            order.PaymentStatus = PaymentStatuses.Paid;
            order.PaidAt = DateTime.UtcNow;
            _db.OrderPayments.Add(new OrderPayment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Kind = PaymentKinds.CodCapture,
                Method = PaymentMethods.COD,
                Status = PaymentStatuses.Paid,
                Note = "Thu COD khi hoàn thành đơn (demo)."
            });
        }

        _audit.AddStatusChange(order.Id, from, body.Status, sellerId, Roles.Seller, null);

        await _db.SaveChangesAsync();

        await _notify.BroadcastOrderAsync(order);
        if (body.Status == OrderStatuses.Delivering && !string.IsNullOrWhiteSpace(order.Phone))
            _notify.LogSmsStub(order.Phone!, $"Đơn #{order.Id} đang giao. Mã vận đơn: {order.TrackingNumber}");

        return Ok(new { message = "Đã cập nhật trạng thái.", order.Id, order.Status, order.TrackingNumber });
    }

    public class RejectOrderRequest
    {
        public string? Reason { get; set; }
    }

    /// <summary>Từ chối đơn khi còn Pending (khác với khách tự hủy).</summary>
    [HttpPost]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectOrderRequest? body)
    {
        var sellerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.RestaurantId == rest.Id);
        if (order == null)
            return NotFound();

        if (order.Status != OrderStatuses.Pending)
            return BadRequest(new { message = "Chỉ từ chối đơn đang chờ xác nhận." });

        var from = order.Status;
        order.Status = OrderStatuses.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelledBy = OrderCancelledBy.Seller;
        if (!string.IsNullOrWhiteSpace(body?.Reason))
        {
            var r = body.Reason.Trim();
            order.CancelReason = r.Length > 500 ? r[..500] : r;
        }

        _audit.AddStatusChange(order.Id, from, OrderStatuses.Cancelled, sellerId, Roles.Seller, order.CancelReason);

        await _db.SaveChangesAsync();

        if (InventoryRules.ShouldRestoreStockOnCancel(from, OrderStatuses.Cancelled))
            await _inventory.RestoreStockForOrderAsync(order.Id);

        await _notify.BroadcastOrderAsync(order);
        _notify.LogEmailStub($"Quán từ chối đơn #{order.Id}", order.CancelReason ?? "");

        return Ok(new { message = "Đã từ chối đơn.", order.Id, order.Status });
    }
}
