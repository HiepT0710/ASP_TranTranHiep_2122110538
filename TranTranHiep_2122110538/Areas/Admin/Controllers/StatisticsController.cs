using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Areas.Admin.Controllers;

[Area("Admin")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("[area]/[controller]/[action]")]
public class StatisticsController : Controller
{
    private readonly AppDbContext _db;

    public StatisticsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Summary()
    {
        var users = await _db.Users.CountAsync(u => u.Role == Roles.User);
        var sellers = await _db.Users.CountAsync(u => u.Role == Roles.Seller);
        var admins = await _db.Users.CountAsync(u => u.Role == Roles.Admin);
        var lockedUsers = await _db.Users.CountAsync(u => u.IsLocked);
        var restaurants = await _db.Restaurants.CountAsync();
        var restaurantsApproved = await _db.Restaurants.CountAsync(r => r.Status == RestaurantStatuses.Approved);
        var restaurantsPending = await _db.Restaurants.CountAsync(r => r.Status == RestaurantStatuses.Pending);
        var restaurantsSuspended = await _db.Restaurants.CountAsync(r => r.Status == RestaurantStatuses.Suspended);
        var foods = await _db.Foods.CountAsync();
        var orders = await _db.Orders.CountAsync();
        var revenue = await _db.Orders
            .Where(o => o.Status == OrderStatuses.Completed && o.PaymentStatus != PaymentStatuses.Refunded)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var refunds = await _db.Orders.CountAsync(o => o.PaymentStatus == PaymentStatuses.Refunded);
        var reports = await _db.ModerationReports.CountAsync();
        var auditLogs = await _db.AuditLogs.CountAsync();
        var systemSettings = await _db.SystemSettings.CountAsync();

        var ordersByStatus = await _db.Orders.AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new { status = g.Key, count = g.Count() })
            .ToListAsync();

        var topRestaurants = await _db.Orders.AsNoTracking()
            .GroupBy(o => new { o.RestaurantId, o.Restaurant!.Name })
            .Select(g => new { restaurantId = g.Key.RestaurantId, restaurantName = g.Key.Name, orderCount = g.Count(), revenue = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.revenue)
            .Take(10)
            .ToListAsync();

        var topFoods = await _db.OrderDetails.AsNoTracking()
            .GroupBy(x => new { x.FoodId, x.Food!.Name })
            .Select(g => new { foodId = g.Key.FoodId, foodName = g.Key.Name, sold = g.Sum(x => x.Quantity), revenue = g.Sum(x => x.Quantity * x.Price) })
            .OrderByDescending(x => x.sold)
            .Take(10)
            .ToListAsync();

        var topSellers = await _db.Orders.AsNoTracking()
            .GroupBy(o => new { o.Restaurant!.OwnerId, o.Restaurant!.Owner!.Username })
            .Select(g => new { sellerId = g.Key.OwnerId, sellerName = g.Key.Username, orderCount = g.Count(), revenue = g.Sum(x => x.TotalAmount) })
            .OrderByDescending(x => x.revenue)
            .Take(10)
            .ToListAsync();

        var sixMonthsAgo = DateTime.UtcNow.Date.AddMonths(-6);
        var recentOrders = await _db.Orders.AsNoTracking()
            .Where(o => o.OrderDate >= sixMonthsAgo)
            .Select(o => new { o.OrderDate, o.Status, o.TotalAmount, o.PaymentStatus })
            .ToListAsync();

        var ordersByMonth = recentOrders
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                year = g.Key.Year,
                month = g.Key.Month,
                orderCount = g.Count(),
                revenueCompleted = g.Where(x => x.Status == OrderStatuses.Completed && x.PaymentStatus != PaymentStatuses.Refunded).Sum(x => x.TotalAmount)
            })
            .ToList();

        var reviewsCount = await _db.FoodReviews.CountAsync();

        return Ok(new
        {
            users,
            sellers,
            admins,
            lockedUsers,
            restaurants,
            restaurantsApproved,
            restaurantsPending,
            restaurantsSuspended,
            foods,
            orders,
            revenueCompletedOrders = revenue,
            refundedOrdersCount = refunds,
            reviewsCount,
            reports,
            auditLogs,
            systemSettings,
            ordersByStatus,
            ordersByMonthLast6Months = ordersByMonth,
            topRestaurants,
            topFoods,
            topSellers
        });
    }
}
