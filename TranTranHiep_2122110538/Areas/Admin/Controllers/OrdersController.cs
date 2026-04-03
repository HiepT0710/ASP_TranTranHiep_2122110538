using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Hubs;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Areas.Admin.Controllers;

[Area("Admin")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
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
                o.Phone
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
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

        order.Status = body.Status;
        await _db.SaveChangesAsync();

        await _hub.Clients.Group(OrderHub.UserGroupName(order.UserId.ToString()))
            .SendAsync("OrderStatusChanged", new { order.Id, order.Status, order.TotalAmount, order.RestaurantId });

        return Ok(new { message = "Đã cập nhật trạng thái.", order.Id, order.Status });
    }
}
