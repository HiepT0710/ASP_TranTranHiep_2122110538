using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Infrastructure;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Services;

public class UserCartService : IUserCartService
{
    private const string CartKey = "Cart";
    private readonly AppDbContext _db;

    public UserCartService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<CartItemDto>> GetCartLinesAsync(HttpContext http)
    {
        var userId = GetUserId(http);
        if (userId != null)
        {
            return await _db.CartItems.AsNoTracking()
                .Where(c => c.UserId == userId.Value)
                .Select(c => new CartItemDto { FoodId = c.FoodId, Quantity = c.Quantity })
                .ToListAsync();
        }

        return http.Session.GetJson<List<CartItemDto>>(CartKey) ?? new List<CartItemDto>();
    }

    public async Task MergeSessionIntoDatabaseAsync(HttpContext http, int userId)
    {
        var sessionCart = http.Session.GetJson<List<CartItemDto>>(CartKey) ?? new List<CartItemDto>();
        if (sessionCart.Count == 0)
            return;

        foreach (var line in sessionCart)
        {
            var food = await _db.Foods.FirstOrDefaultAsync(f => f.Id == line.FoodId && f.IsAvailable);
            if (food == null)
                continue;

            var maxQty = Math.Min(line.Quantity, Math.Max(0, food.StockQuantity));
            if (maxQty <= 0)
                continue;

            var existing = await _db.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.FoodId == line.FoodId);
            if (existing != null)
            {
                var combined = existing.Quantity + maxQty;
                existing.Quantity = Math.Min(combined, Math.Max(0, food.StockQuantity));
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    FoodId = line.FoodId,
                    Quantity = maxQty,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
        http.Session.SetJson(CartKey, new List<CartItemDto>());
    }

    public async Task ClearDatabaseCartAsync(int userId)
    {
        await _db.CartItems.Where(c => c.UserId == userId).ExecuteDeleteAsync();
    }

    private static int? GetUserId(HttpContext http)
    {
        var id = http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(id, out var v) ? v : null;
    }
}
