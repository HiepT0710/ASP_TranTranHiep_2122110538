using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Areas.Seller.Controllers;

[Area("Seller")]
[ApiController]
[Authorize(Roles = Roles.Seller)]
[Route("[area]/[controller]/[action]")]
public class StatisticsController : Controller
{
    private readonly AppDbContext _db;

    public StatisticsController(AppDbContext db)
    {
        _db = db;
    }

    private async Task<Restaurant?> GetMyRestaurantAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.OwnerId == userId);
    }

    /// <summary>Thống kê theo quán của Seller (doanh thu, đơn theo trạng thái, đơn hôm nay).</summary>
    [HttpGet]
    public async Task<IActionResult> Summary()
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var rid = rest.Id;
        var ordersTotal = await _db.Orders.CountAsync(o => o.RestaurantId == rid);

        var byStatus = await _db.Orders.AsNoTracking()
            .Where(o => o.RestaurantId == rid)
            .GroupBy(o => o.Status)
            .Select(g => new { status = g.Key, count = g.Count() })
            .ToListAsync();

        var revenue = await _db.Orders
            .Where(o => o.RestaurantId == rid
                && o.Status == OrderStatuses.Completed
                && o.PaymentStatus != PaymentStatuses.Refunded)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var today = DateTime.UtcNow.Date;
        var ordersToday = await _db.Orders.CountAsync(o => o.RestaurantId == rid && o.OrderDate >= today);

        var foodsCount = await _db.Foods.CountAsync(f => f.RestaurantId == rid);
        var categoriesCount = await _db.Categories.CountAsync(c => c.RestaurantId == rid);

        var avgOrderValue = await _db.Orders
            .Where(o => o.RestaurantId == rid && o.Status == OrderStatuses.Completed)
            .Select(o => (decimal?)o.TotalAmount)
            .AverageAsync() ?? 0;

        return Ok(new
        {
            restaurantId = rid,
            restaurantName = rest.Name,
            ordersTotal,
            ordersToday,
            revenueCompletedExcludingRefunds = revenue,
            foodsCount,
            categoriesCount,
            averageCompletedOrderValue = avgOrderValue,
            ordersByStatus = byStatus
        });
    }
}
