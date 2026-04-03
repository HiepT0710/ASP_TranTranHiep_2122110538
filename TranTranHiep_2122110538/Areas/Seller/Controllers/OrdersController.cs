using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Hubs;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Areas.Seller.Controllers;

[Area("Seller")]
[ApiController]
[Authorize(Roles = Roles.Seller)]
[Route("[area]/[controller]/[action]/{id?}")]
public class OrdersController : Controller
{
    private readonly AppDbContext _db;
    private readonly IHubContext<OrderHub> _hub;

    public OrdersController(AppDbContext db, IHubContext<OrderHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    private async Task<Restaurant?> GetMyRestaurantAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _db.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == userId);
    }

    /// <summary>Chỉ đơn của quán mình.</summary>
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
                o.Phone
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, restaurantId = rest.Id, items });
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    /// <summary>Cập nhật trạng thái: Đang chuẩn bị / Đang giao / Hoàn thành (theo OrderStatuses.SellerAssignable).</summary>
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest body)
    {
        if (!OrderStatuses.SellerAssignable.Contains(body.Status))
            return BadRequest(new { message = "Seller chỉ được đặt: Preparing, Delivering, Completed." });

        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.RestaurantId == rest.Id);
        if (order == null)
            return NotFound();

        order.Status = body.Status;
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(OrderHub.UserGroupName(order.UserId.ToString()))
            .SendAsync("OrderStatusChanged", new { order.Id, order.Status, order.TotalAmount, order.RestaurantId });

        return Ok(new { message = "Đã cập nhật trạng thái.", order.Id, order.Status });
    }
}
