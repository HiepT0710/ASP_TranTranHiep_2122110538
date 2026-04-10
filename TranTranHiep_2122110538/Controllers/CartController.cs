using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Infrastructure;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.Services;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Controllers;

/// <summary>Giỏ hàng: session (khách) hoặc bảng CartItems (đã đăng nhập — đồng bộ thiết bị).</summary>
[ApiController]
[AllowAnonymous]
[Route("[controller]/[action]")]
public class CartController : Controller
{
    private const string CartKey = "Cart";
    private readonly AppDbContext _db;
    private readonly IUserCartService _userCart;

    public CartController(AppDbContext db, IUserCartService userCart)
    {
        _db = db;
        _userCart = userCart;
    }

    private List<CartItemDto> GetSessionCart() =>
        HttpContext.Session.GetJson<List<CartItemDto>>(CartKey) ?? new List<CartItemDto>();

    private void SaveSessionCart(List<CartItemDto> cart) =>
        HttpContext.Session.SetJson(CartKey, cart);

    private int? CurrentUserId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private async Task<List<CartItemDto>> GetCartAsync()
    {
        if (CurrentUserId != null)
            return await _userCart.GetCartLinesAsync(HttpContext);
        return GetSessionCart();
    }

    private int CartItemCount(IEnumerable<CartItemDto> cart) => cart.Sum(x => x.Quantity);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = await GetCartAsync();
        if (cart.Count == 0)
            return Ok(new { items = Array.Empty<object>(), totalQuantity = 0, subtotal = 0m, storage = CurrentUserId != null ? "database" : "session" });

        var ids = cart.Select(c => c.FoodId).Distinct().ToList();
        var foods = await _db.Foods.AsNoTracking()
            .Where(f => ids.Contains(f.Id) && f.IsAvailable)
            .ToDictionaryAsync(f => f.Id);

        var lines = new List<object>();
        decimal subtotal = 0;
        foreach (var line in cart)
        {
            if (!foods.TryGetValue(line.FoodId, out var food))
                continue;
            var lineTotal = food.Price * line.Quantity;
            subtotal += lineTotal;
            lines.Add(new
            {
                food.Id,
                food.Name,
                food.Price,
                food.Image,
                food.StockQuantity,
                line.Quantity,
                lineTotal
            });
        }

        return Ok(new
        {
            items = lines,
            totalQuantity = CartItemCount(cart),
            subtotal,
            storage = CurrentUserId != null ? "database" : "session"
        });
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] CartAddRequest model)
    {
        if (model.Quantity <= 0)
            return BadRequest(new { message = "Số lượng phải > 0." });

        var food = await _db.Foods.FirstOrDefaultAsync(f => f.Id == model.FoodId && f.IsAvailable);
        if (food == null)
            return NotFound(new { message = "Món không tồn tại hoặc ngừng bán." });

        var uid = CurrentUserId;
        if (uid != null)
        {
            var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == uid && c.FoodId == model.FoodId);
            var newQty = (existing?.Quantity ?? 0) + model.Quantity;
            if (newQty > food.StockQuantity)
                return BadRequest(new { message = $"Chỉ còn {food.StockQuantity} phần trong kho." });

            if (existing != null)
            {
                existing.Quantity = newQty;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    UserId = uid.Value,
                    FoodId = model.FoodId,
                    Quantity = model.Quantity,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            var list = await _userCart.GetCartLinesAsync(HttpContext);
            return Ok(new { message = "Đã thêm vào giỏ.", totalQuantity = CartItemCount(list), storage = "database" });
        }

        var cart = GetSessionCart();
        var ex = cart.FirstOrDefault(c => c.FoodId == model.FoodId);
        var q = (ex?.Quantity ?? 0) + model.Quantity;
        if (q > food.StockQuantity)
            return BadRequest(new { message = $"Chỉ còn {food.StockQuantity} phần trong kho." });

        if (ex != null)
            ex.Quantity = q;
        else
            cart.Add(new CartItemDto { FoodId = model.FoodId, Quantity = model.Quantity });

        SaveSessionCart(cart);
        return Ok(new { message = "Đã thêm vào giỏ.", totalQuantity = CartItemCount(cart), storage = "session" });
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] CartUpdateRequest model)
    {
        var uid = CurrentUserId;
        if (uid != null)
        {
            var line = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == uid && c.FoodId == model.FoodId);
            if (line == null)
                return NotFound(new { message = "Không có trong giỏ." });

            if (model.Quantity <= 0)
            {
                _db.CartItems.Remove(line);
            }
            else
            {
                var food = await _db.Foods.FirstOrDefaultAsync(f => f.Id == model.FoodId && f.IsAvailable);
                if (food == null)
                    return BadRequest(new { message = "Món không khả dụng." });
                if (model.Quantity > food.StockQuantity)
                    return BadRequest(new { message = $"Tối đa {food.StockQuantity} phần." });
                line.Quantity = model.Quantity;
                line.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            var list = await _userCart.GetCartLinesAsync(HttpContext);
            return Ok(new { message = "Đã cập nhật.", totalQuantity = CartItemCount(list), storage = "database" });
        }

        var cart = GetSessionCart();
        var sessionLine = cart.FirstOrDefault(c => c.FoodId == model.FoodId);
        if (sessionLine == null)
            return NotFound(new { message = "Không có trong giỏ." });

        if (model.Quantity <= 0)
            cart.Remove(sessionLine);
        else
        {
            var food = await _db.Foods.FirstOrDefaultAsync(f => f.Id == model.FoodId && f.IsAvailable);
            if (food == null)
                return BadRequest(new { message = "Món không khả dụng." });
            if (model.Quantity > food.StockQuantity)
                return BadRequest(new { message = $"Tối đa {food.StockQuantity} phần." });
            sessionLine.Quantity = model.Quantity;
        }

        SaveSessionCart(cart);
        return Ok(new { message = "Đã cập nhật.", totalQuantity = CartItemCount(cart), storage = "session" });
    }

    [HttpPost]
    public async Task<IActionResult> Remove([FromBody] CartAddRequest model)
    {
        var uid = CurrentUserId;
        if (uid != null)
        {
            var line = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == uid && c.FoodId == model.FoodId);
            if (line != null)
            {
                _db.CartItems.Remove(line);
                await _db.SaveChangesAsync();
            }

            var list = await _userCart.GetCartLinesAsync(HttpContext);
            return Ok(new { message = "Đã xóa.", totalQuantity = CartItemCount(list), storage = "database" });
        }

        var cart = GetSessionCart();
        cart.RemoveAll(c => c.FoodId == model.FoodId);
        SaveSessionCart(cart);
        return Ok(new { message = "Đã xóa.", totalQuantity = CartItemCount(cart), storage = "session" });
    }

    [HttpPost]
    public async Task<IActionResult> Clear()
    {
        var uid = CurrentUserId;
        if (uid != null)
        {
            await _userCart.ClearDatabaseCartAsync(uid.Value);
            return Ok(new { message = "Đã xóa giỏ.", totalQuantity = 0, storage = "database" });
        }

        SaveSessionCart(new List<CartItemDto>());
        return Ok(new { message = "Đã xóa giỏ.", totalQuantity = 0, storage = "session" });
    }
}
