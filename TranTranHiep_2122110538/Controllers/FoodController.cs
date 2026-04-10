using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Controllers;

[ApiController]
[AllowAnonymous]
[Route("[controller]/[action]/{id?}")]
public class FoodController : Controller
{
    private readonly AppDbContext _db;

    public FoodController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Danh mục của một quán (để lọc món trên UI).</summary>
    [HttpGet]
    public async Task<IActionResult> Categories(int restaurantId)
    {
        var ok = await _db.Restaurants.AsNoTracking()
            .AnyAsync(r => r.Id == restaurantId && r.Status == RestaurantStatuses.Approved);
        if (!ok)
            return NotFound(new { message = "Không tìm thấy quán." });

        var items = await _db.Categories.AsNoTracking()
            .Where(c => c.RestaurantId == restaurantId)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Description })
            .ToListAsync();

        return Ok(new { restaurantId, items });
    }

    /// <summary>Danh sách món từ quán đã duyệt — phân trang, lọc danh mục, tìm theo tên.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 8, int? categoryId = null, int? restaurantId = null, string? q = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 10);

        var query = _db.Foods.AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Restaurant)
            .Where(f => f.IsAvailable && f.Restaurant!.Status == RestaurantStatuses.Approved);

        if (restaurantId.HasValue)
            query = query.Where(f => f.RestaurantId == restaurantId.Value);

        if (categoryId.HasValue)
            query = query.Where(f => f.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(f => f.Name.Contains(term));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(f => f.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Price,
                f.Image,
                f.Description,
                f.CategoryId,
                CategoryName = f.Category!.Name,
                f.RestaurantId,
                RestaurantName = f.Restaurant!.Name,
                f.IsAvailable,
                f.StockQuantity
            })
            .ToListAsync();

        return Ok(new
        {
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize),
            items
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var food = await _db.Foods.AsNoTracking()
            .Include(f => f.Category)
            .Include(f => f.Restaurant)
            .Where(f => f.Id == id && f.Restaurant!.Status == RestaurantStatuses.Approved)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.Price,
                f.Image,
                f.Description,
                f.CategoryId,
                CategoryName = f.Category!.Name,
                f.RestaurantId,
                RestaurantName = f.Restaurant!.Name,
                f.IsAvailable,
                f.StockQuantity
            })
            .FirstOrDefaultAsync();

        if (food == null)
            return NotFound(new { message = "Không tìm thấy món." });

        var reviewCount = await _db.FoodReviews.AsNoTracking().CountAsync(r => r.FoodId == id);
        double? avgRating = reviewCount == 0
            ? null
            : await _db.FoodReviews.AsNoTracking().Where(r => r.FoodId == id).AverageAsync(r => (double)r.Rating);

        return Ok(new
        {
            food.Id,
            food.Name,
            food.Price,
            food.Image,
            food.Description,
            food.CategoryId,
            food.CategoryName,
            food.RestaurantId,
            food.RestaurantName,
            food.IsAvailable,
            food.StockQuantity,
            reviewCount,
            avgRating
        });
    }

    /// <summary>Đánh giá công khai theo món (phân trang).</summary>
    [HttpGet]
    public async Task<IActionResult> Reviews(int foodId, int page = 1, int pageSize = 10)
    {
        var exists = await _db.Foods.AsNoTracking()
            .AnyAsync(f => f.Id == foodId && f.IsAvailable && f.Restaurant!.Status == RestaurantStatuses.Approved);
        if (!exists)
            return NotFound(new { message = "Không tìm thấy món." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);

        var query = _db.FoodReviews.AsNoTracking()
            .Where(r => r.FoodId == foodId)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new
            {
                r.Rating,
                r.Comment,
                r.CreatedAt,
                Username = r.User!.Username
            })
            .ToListAsync();

        return Ok(new { foodId, page, pageSize, total, items });
    }
}
