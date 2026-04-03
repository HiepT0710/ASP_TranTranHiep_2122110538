using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Infrastructure;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Controllers;

/// <summary>Giỏ hàng lưu trong Session (JSON).</summary>
[ApiController]
[AllowAnonymous]
[Route("[controller]/[action]")]
public class CartController : Controller
{
    private const string CartKey = "Cart";
    private readonly AppDbContext _db;

    public CartController(AppDbContext db)
    {
        _db = db;
    }

    private List<CartItemDto> GetCart()
    {
        return HttpContext.Session.GetJson<List<CartItemDto>>(CartKey) ?? new List<CartItemDto>();
    }

    private void SaveCart(List<CartItemDto> cart)
    {
        HttpContext.Session.SetJson(CartKey, cart);
    }

    private int CartItemCount(IEnumerable<CartItemDto> cart) => cart.Sum(x => x.Quantity);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var cart = GetCart();
        if (cart.Count == 0)
            return Ok(new { items = Array.Empty<object>(), totalQuantity = 0, subtotal = 0m });

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
                line.Quantity,
                lineTotal
            });
        }

        return Ok(new
        {
            items = lines,
            totalQuantity = CartItemCount(cart),
            subtotal
        });
    }

    /// <summary>Thêm vào giỏ (AJAX — không cần reload trang khi gọi từ client).</summary>
    [HttpPost]
    public async Task<IActionResult> Add([FromBody] CartAddRequest model)
    {
        if (model.Quantity <= 0)
            return BadRequest(new { message = "Số lượng phải > 0." });

        var food = await _db.Foods.FirstOrDefaultAsync(f => f.Id == model.FoodId && f.IsAvailable);
        if (food == null)
            return NotFound(new { message = "Món không tồn tại hoặc ngừng bán." });

        var cart = GetCart();
        var existing = cart.FirstOrDefault(c => c.FoodId == model.FoodId);
        if (existing != null)
            existing.Quantity += model.Quantity;
        else
            cart.Add(new CartItemDto { FoodId = model.FoodId, Quantity = model.Quantity });

        SaveCart(cart);
        return Ok(new
        {
            message = "Đã thêm vào giỏ.",
            totalQuantity = CartItemCount(cart)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Update([FromBody] CartUpdateRequest model)
    {
        var cart = GetCart();
        var line = cart.FirstOrDefault(c => c.FoodId == model.FoodId);
        if (line == null)
            return NotFound(new { message = "Không có trong giỏ." });

        if (model.Quantity <= 0)
        {
            cart.Remove(line);
        }
        else
        {
            var food = await _db.Foods.AnyAsync(f => f.Id == model.FoodId && f.IsAvailable);
            if (!food)
                return BadRequest(new { message = "Món không khả dụng." });
            line.Quantity = model.Quantity;
        }

        SaveCart(cart);
        return Ok(new { message = "Đã cập nhật.", totalQuantity = CartItemCount(cart) });
    }

    [HttpPost]
    public IActionResult Remove([FromBody] CartAddRequest model)
    {
        var cart = GetCart();
        cart.RemoveAll(c => c.FoodId == model.FoodId);
        SaveCart(cart);
        return Ok(new { message = "Đã xóa.", totalQuantity = CartItemCount(cart) });
    }

    [HttpPost]
    public IActionResult Clear()
    {
        SaveCart(new List<CartItemDto>());
        return Ok(new { message = "Đã xóa giỏ." });
    }
}
