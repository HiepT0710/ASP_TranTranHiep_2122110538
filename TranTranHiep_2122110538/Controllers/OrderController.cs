using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Infrastructure;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Controllers;

[ApiController]
[Authorize]
[Route("[controller]/[action]/{id?}")]
public class OrderController : Controller
{
    private const string CartKey = "Cart";
    private readonly AppDbContext _db;

    public OrderController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest? body)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var cart = HttpContext.Session.GetJson<List<CartItemDto>>(CartKey) ?? new List<CartItemDto>();
        if (cart.Count == 0)
            return BadRequest(new { message = "Giỏ hàng trống." });

        var ids = cart.Select(c => c.FoodId).ToList();
        var foods = await _db.Foods
            .Include(f => f.Restaurant)
            .Where(f => ids.Contains(f.Id) && f.IsAvailable)
            .ToDictionaryAsync(f => f.Id);

        decimal total = 0;
        var lines = new List<OrderDetail>();
        int? restaurantId = null;

        foreach (var item in cart)
        {
            if (!foods.TryGetValue(item.FoodId, out var food))
                return BadRequest(new { message = $"Món Id {item.FoodId} không hợp lệ." });

            if (food.Restaurant!.Status != RestaurantStatuses.Approved)
                return BadRequest(new { message = $"Quán của món \"{food.Name}\" chưa được duyệt hoặc không hoạt động." });

            if (restaurantId == null)
                restaurantId = food.RestaurantId;
            else if (restaurantId != food.RestaurantId)
                return BadRequest(new { message = "Một đơn chỉ chứa món từ một quán. Hãy đặt từng quán riêng." });

            var lineTotal = food.Price * item.Quantity;
            total += lineTotal;
            lines.Add(new OrderDetail
            {
                FoodId = food.Id,
                Quantity = item.Quantity,
                Price = food.Price
            });
        }

        if (restaurantId == null)
            return BadRequest(new { message = "Không xác định được quán." });

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        var order = new Order
        {
            UserId = userId,
            RestaurantId = restaurantId.Value,
            OrderDate = DateTime.UtcNow,
            TotalAmount = total,
            Status = OrderStatuses.Pending,
            Address = body?.Address ?? user?.Address,
            Phone = body?.Phone ?? user?.Phone
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        foreach (var line in lines)
            line.OrderId = order.Id;

        _db.OrderDetails.AddRange(lines);
        await _db.SaveChangesAsync();

        HttpContext.Session.SetJson(CartKey, new List<CartItemDto>());

        return Ok(new { message = "Đặt hàng thành công.", orderId = order.Id, total = order.TotalAmount, restaurantId = order.RestaurantId });
    }

    [HttpGet]
    public async Task<IActionResult> MyOrders(int page = 1, int pageSize = 10)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Orders.AsNoTracking()
            .Include(o => o.Restaurant)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
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

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole(Roles.Admin);
        var isSeller = User.IsInRole(Roles.Seller);
        int? sellerRestaurantId = null;
        if (isSeller)
        {
            var rid = User.FindFirstValue(AuthClaims.RestaurantId);
            if (int.TryParse(rid, out var parsed))
                sellerRestaurantId = parsed;
            else
                sellerRestaurantId = await _db.Restaurants.AsNoTracking()
                    .Where(r => r.OwnerId == userId)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefaultAsync();
        }

        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Food)
            .Include(o => o.Restaurant)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn." });

        if (order.UserId != userId && !isAdmin && !(isSeller && sellerRestaurantId == order.RestaurantId))
            return StatusCode(403, new { message = "Không xem được đơn này." });

        return Ok(new
        {
            order.Id,
            order.RestaurantId,
            RestaurantName = order.Restaurant?.Name,
            order.OrderDate,
            order.TotalAmount,
            order.Status,
            order.Address,
            order.Phone,
            details = order.OrderDetails.Select(od => new
            {
                od.FoodId,
                FoodName = od.Food!.Name,
                od.Quantity,
                od.Price,
                lineTotal = od.Price * od.Quantity
            })
        });
    }
}
