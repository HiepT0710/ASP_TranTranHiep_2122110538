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
        var restaurants = await _db.Restaurants.CountAsync();
        var restaurantsApproved = await _db.Restaurants.CountAsync(r => r.Status == RestaurantStatuses.Approved);
        var restaurantsPending = await _db.Restaurants.CountAsync(r => r.Status == RestaurantStatuses.Pending);
        var foods = await _db.Foods.CountAsync();
        var orders = await _db.Orders.CountAsync();
        var revenue = await _db.Orders
            .Where(o => o.Status == OrderStatuses.Completed && o.PaymentStatus != PaymentStatuses.Refunded)
            .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

        var refunds = await _db.Orders.CountAsync(o => o.PaymentStatus == PaymentStatuses.Refunded);

        var ordersByStatus = await _db.Orders.AsNoTracking()
            .GroupBy(o => o.Status)
            .Select(g => new { status = g.Key, count = g.Count() })
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
                revenueCompleted = g
                    .Where(x => x.Status == OrderStatuses.Completed && x.PaymentStatus != PaymentStatuses.Refunded)
                    .Sum(x => x.TotalAmount)
            })
            .ToList();

        var reviewsCount = await _db.FoodReviews.CountAsync();

        return Ok(new
        {
            users,
            sellers,
            admins,
            restaurants,
            restaurantsApproved,
            restaurantsPending,
            foods,
            orders,
            revenueCompletedOrders = revenue,
            refundedOrdersCount = refunds,
            reviewsCount,
            ordersByStatus,
            ordersByMonthLast6Months = ordersByMonth
        });
    }
}
