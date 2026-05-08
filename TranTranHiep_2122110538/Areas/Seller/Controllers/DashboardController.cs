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
public class DashboardController : Controller
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    private async Task<Restaurant?> GetMyRestaurantAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.OwnerId == userId);
    }

    [HttpGet]
    public async Task<IActionResult> Summary(DateTime? from = null, DateTime? to = null, string bucket = "day")
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var rid = rest.Id;
        var fromUtc = from?.Date ?? DateTime.UtcNow.Date.AddDays(-13);
        var toUtc = to?.Date.AddDays(1).AddTicks(-1) ?? DateTime.UtcNow;
        var orders = _db.Orders.AsNoTracking().Where(o => o.RestaurantId == rid && o.OrderDate >= fromUtc && o.OrderDate <= toUtc);

        var totalOrders = await orders.CountAsync();
        var revenueByDay = await orders
            .Where(o => o.Status == OrderStatuses.Completed)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { day = g.Key, total = g.Sum(x => x.TotalAmount) })
            .OrderBy(x => x.day)
            .ToListAsync();

        var revenueByMonth = await orders
            .Where(o => o.Status == OrderStatuses.Completed)
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, total = g.Sum(x => x.TotalAmount) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        var successCount = await orders.CountAsync(o => o.Status == OrderStatuses.Completed);
        var cancelledCount = await orders.CountAsync(o => o.Status == OrderStatuses.Cancelled);
        var bestSellers = await _db.OrderDetails.AsNoTracking()
            .Where(od => od.Order!.RestaurantId == rid && od.Order!.OrderDate >= fromUtc && od.Order!.OrderDate <= toUtc)
            .GroupBy(od => new { od.FoodId, FoodName = od.Food!.Name })
            .Select(g => new { g.Key.FoodId, g.Key.FoodName, quantity = g.Sum(x => x.Quantity) })
            .OrderByDescending(x => x.quantity)
            .Take(5)
            .ToListAsync();

        var revenueSeries = bucket == "month" ? revenueByMonth.Select(x => new { label = $"{x.Month:00}/{x.Year}", total = x.total }) : revenueByDay.Select(x => new { label = x.day.ToString("dd/MM"), total = x.total });

        return Ok(new
        {
            restaurantId = rid,
            restaurantName = rest.Name,
            from = fromUtc,
            to = toUtc,
            bucket,
            totalOrders,
            revenueByDay,
            revenueByMonth,
            revenueSeries,
            successCount,
            cancelledCount,
            bestSellers,
            conversionRate = totalOrders > 0 ? (decimal)successCount / totalOrders : 0m
        });
    }
}
