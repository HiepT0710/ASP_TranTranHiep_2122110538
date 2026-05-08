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
    public async Task<IActionResult> Categories(int? restaurantId = null, int? id = null)
    {
        var targetRestaurantId = restaurantId ?? id;
        if (!targetRestaurantId.HasValue)
            return BadRequest(new { message = "Thiếu restaurantId." });

        var ok = await _db.Restaurants.AsNoTracking()
            .AnyAsync(r => r.Id == targetRestaurantId.Value && r.Status == RestaurantStatuses.Approved);
        if (!ok)
            return NotFound(new { message = "Không tìm thấy quán." });

        var items = await _db.Categories.AsNoTracking()
            .Where(c => c.RestaurantId == targetRestaurantId.Value)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Description })
            .ToListAsync();

        return Ok(new { restaurantId = targetRestaurantId.Value, items });
    }

    /// <summary>Danh sách món từ quán đã duyệt — phân trang, lọc danh mục, tìm theo tên.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 8, int? categoryId = null, int? restaurantId = null, string? q = null, string? sortBy = null)
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

        if (string.Equals(sortBy, "rating_desc", StringComparison.OrdinalIgnoreCase))
        {
            var allRows = await query
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
                    f.IsOnSale,
                    f.SalePercent,
                    f.StockQuantity,
                    reviewCount = f.Reviews.Count,
                    avgRating = f.Reviews.Count == 0 ? (double?)null : f.Reviews.Average(r => (double)r.Rating)
                })
                .ToListAsync();

            var ratingTotalRows = allRows.Count;
            var ratingItems = allRows
                .OrderByDescending(f => f.avgRating ?? 0)
                .ThenBy(f => f.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new
            {
                page,
                pageSize,
                total = ratingTotalRows,
                totalPages = (int)Math.Ceiling(ratingTotalRows / (double)pageSize),
                items = ratingItems
            });
        }

        if (string.Equals(sortBy, "name_desc", StringComparison.OrdinalIgnoreCase))
            query = query.OrderByDescending(f => f.Name);
        else if (string.Equals(sortBy, "price_asc", StringComparison.OrdinalIgnoreCase))
            query = query.OrderBy(f => f.Price);
        else if (string.Equals(sortBy, "price_desc", StringComparison.OrdinalIgnoreCase))
            query = query.OrderByDescending(f => f.Price);
        else
            query = query.OrderBy(f => f.Name);

        var total = await query.CountAsync();
        var items = await query
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
                f.IsOnSale,
                f.SalePercent,
                f.StockQuantity,
                reviewCount = f.Reviews.Count,
                avgRating = f.Reviews.Count == 0 ? (double?)null : f.Reviews.Average(r => (double)r.Rating)
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
                f.IsOnSale,
                f.SalePercent,
                f.StockQuantity
            })
            .FirstOrDefaultAsync();

        if (food == null)
            return NotFound(new { message = "Không tìm thấy món." });

        var reviewCount = 0;
        double? avgRating = null;
        try
        {
            reviewCount = await _db.FoodReviews.AsNoTracking().CountAsync(r => r.FoodId == id);
            avgRating = reviewCount == 0
                ? null
                : await _db.FoodReviews.AsNoTracking().Where(r => r.FoodId == id).AverageAsync(r => (double)r.Rating);
        }
        catch
        {
            reviewCount = 0;
            avgRating = null;
        }

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
            food.IsOnSale,
            food.SalePercent,
            food.StockQuantity,
            reviewCount,
            avgRating
        });
    }

    /// <summary>Đánh giá công khai theo món (phân trang).</summary>
    [HttpGet]
    public async Task<IActionResult> Reviews(int? foodId = null, int? id = null, int page = 1, int pageSize = 10, int? rating = null)
    {
        var targetFoodId = foodId ?? id;
        if (!targetFoodId.HasValue)
            return BadRequest(new { message = "Thiếu foodId." });

        try
        {
            var exists = await _db.Foods.AsNoTracking()
                .AnyAsync(f => f.Id == targetFoodId.Value && f.IsAvailable && f.Restaurant!.Status == RestaurantStatuses.Approved);
            if (!exists)
                return NotFound(new { message = "Không tìm thấy món." });

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 50);

            var query = _db.FoodReviews.AsNoTracking()
                .Where(r => r.FoodId == targetFoodId.Value);

            if (rating.HasValue)
                query = query.Where(r => r.Rating == rating.Value);

            query = query.OrderByDescending(r => r.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.Rating,
                    r.Comment,
                    r.ImageUrlsJson,
                    r.CreatedAt,
                    Username = r.User != null ? r.User.Username : "Ẩn danh"
                })
                .ToListAsync();

            return Ok(new { foodId = targetFoodId.Value, page, pageSize, total, items });
        }
        catch
        {
            return Ok(new { foodId = targetFoodId.Value, page = Math.Max(1, page), pageSize = Math.Clamp(pageSize, 5, 50), total = 0, items = Array.Empty<object>() });
        }
    }
}
