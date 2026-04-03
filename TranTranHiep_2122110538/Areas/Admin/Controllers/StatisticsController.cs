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
        var revenue = await _db.Orders.Where(o => o.Status == OrderStatuses.Completed).SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

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
            revenueCompletedOrders = revenue
        });
    }
}
