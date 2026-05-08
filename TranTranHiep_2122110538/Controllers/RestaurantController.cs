using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Controllers;

/// <summary>Danh sách quán công khai (đã duyệt) — hỗ trợ trang đặt món.</summary>
[ApiController]
[AllowAnonymous]
[Route("[controller]/[action]/{id?}")]
public class RestaurantController : Controller
{
    private readonly AppDbContext _db;

    public RestaurantController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? q = null, string? sortBy = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 50);

        var query = _db.Restaurants.AsNoTracking()
            .Where(r => r.Status == RestaurantStatuses.Approved);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(r => r.Name.Contains(term) || (r.Address != null && r.Address.Contains(term)));
        }

        if (string.Equals(sortBy, "rating_desc", StringComparison.OrdinalIgnoreCase))
        {
            var allRows = await query
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.Address,
                    r.Phone,
                    r.CoverImage,
                    r.GalleryImage1,
                    r.GalleryImage2,
                    r.GalleryImage3,
                    r.IsOnSale,
                    r.SalePercent,
                    AvgRating = r.Reviews.Count == 0 ? 0 : r.Reviews.Average(rv => (double)rv.Rating)
                })
                .ToListAsync();

            var ratingTotalRows = allRows.Count;
            var sortedRows = allRows
                .OrderByDescending(r => r.AvgRating)
                .ThenBy(r => r.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var sortedIds = sortedRows.Select(r => r.Id).ToList();
            var ratingFoodCountRows = await _db.Foods.AsNoTracking()
                .Where(f => sortedIds.Contains(f.RestaurantId) && f.IsAvailable)
                .GroupBy(f => f.RestaurantId)
                .Select(g => new { RestaurantId = g.Key, Count = g.Count() })
                .ToListAsync();
            var ratingFoodCountMap = ratingFoodCountRows.ToDictionary(x => x.RestaurantId, x => x.Count);

            Dictionary<int, dynamic> ratingReviewMap = new();
            try
            {
                var ratingReviewRows = await _db.RestaurantReviews.AsNoTracking()
                    .Where(rv => sortedIds.Contains(rv.RestaurantId))
                    .GroupBy(rv => rv.RestaurantId)
                    .Select(g => new { RestaurantId = g.Key, Count = g.Count(), Avg = g.Average(x => (double)x.Rating) })
                    .ToListAsync();
                ratingReviewMap = ratingReviewRows.ToDictionary(x => x.RestaurantId, x => (dynamic)new { x.Count, Avg = (double?)x.Avg });
            }
            catch
            {
                ratingReviewMap = new Dictionary<int, dynamic>();
            }

            var ratingItems = sortedRows.Select(r => new
            {
                r.Id,
                r.Name,
                r.Address,
                r.Phone,
                r.CoverImage,
                r.GalleryImage1,
                r.GalleryImage2,
                r.GalleryImage3,
                r.IsOnSale,
                r.SalePercent,
                foodCount = ratingFoodCountMap.GetValueOrDefault(r.Id),
                reviewCount = ratingReviewMap.GetValueOrDefault(r.Id)?.Count ?? 0,
                avgRating = ratingReviewMap.GetValueOrDefault(r.Id)?.Avg
            }).ToList();

            return Ok(new { page, pageSize, total = ratingTotalRows, items = ratingItems });
        }

        if (string.Equals(sortBy, "name_desc", StringComparison.OrdinalIgnoreCase))
            query = query.OrderByDescending(r => r.Name);
        else if (string.Equals(sortBy, "newest", StringComparison.OrdinalIgnoreCase))
            query = query.OrderByDescending(r => r.Id);
        else if (string.Equals(sortBy, "oldest", StringComparison.OrdinalIgnoreCase))
            query = query.OrderBy(r => r.Id);
        else
            query = query.OrderBy(r => r.Name);

        var total = await query.CountAsync();
        var pageList = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new { r.Id, r.Name, r.Address, r.Phone, r.CoverImage, r.GalleryImage1, r.GalleryImage2, r.GalleryImage3, r.IsOnSale, r.SalePercent })
            .ToListAsync();

        var rids = pageList.Select(r => r.Id).ToList();
        var countRows = await _db.Foods.AsNoTracking()
            .Where(f => rids.Contains(f.RestaurantId) && f.IsAvailable)
            .GroupBy(f => f.RestaurantId)
            .Select(g => new { RestaurantId = g.Key, Count = g.Count() })
            .ToListAsync();
        var countMap = countRows.ToDictionary(x => x.RestaurantId, x => x.Count);

        Dictionary<int, dynamic> ratingMap = new();
        try
        {
            var ratingRows = await _db.RestaurantReviews.AsNoTracking()
                .Where(rv => rids.Contains(rv.RestaurantId))
                .GroupBy(rv => rv.RestaurantId)
                .Select(g => new { RestaurantId = g.Key, Count = g.Count(), Avg = g.Average(x => (double)x.Rating) })
                .ToListAsync();
            ratingMap = ratingRows.ToDictionary(x => x.RestaurantId, x => (dynamic)new { x.Count, Avg = (double?)x.Avg });
        }
        catch
        {
            ratingMap = new Dictionary<int, dynamic>();
        }

        var items = pageList.Select(r => new
        {
            r.Id,
            r.Name,
            r.Address,
            r.Phone,
            r.CoverImage,
            r.GalleryImage1,
            r.GalleryImage2,
            r.GalleryImage3,
            r.IsOnSale,
            r.SalePercent,
            foodCount = countMap.GetValueOrDefault(r.Id),
            reviewCount = ratingMap.GetValueOrDefault(r.Id)?.Count ?? 0,
            avgRating = ratingMap.GetValueOrDefault(r.Id)?.Avg
        }).ToList();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet]
    public async Task<IActionResult> Sale()
    {
        var items = await _db.Restaurants.AsNoTracking()
            .Where(r => r.Status == RestaurantStatuses.Approved && r.IsOnSale)
            .OrderByDescending(r => r.SalePercent)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Address,
                r.Phone,
                r.CoverImage,
                r.GalleryImage1,
                r.GalleryImage2,
                r.GalleryImage3,
                r.IsOnSale,
                r.SalePercent,
                foodCount = r.Foods.Count(f => f.IsAvailable)
            })
            .Take(12)
            .ToListAsync();

        return Ok(new { items });
    }

    [HttpGet]
    public async Task<IActionResult> BestSellers(int take = 8)
    {
        take = Math.Clamp(take, 1, 20);
        var items = await _db.Foods.AsNoTracking()
            .Where(f => f.IsAvailable && f.Restaurant!.Status == RestaurantStatuses.Approved)
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
                f.IsOnSale,
                f.SalePercent,
                OrdersCount = _db.OrderDetails.Count(od => od.FoodId == f.Id)
            })
            .OrderByDescending(x => x.OrdersCount)
            .ThenByDescending(x => x.IsOnSale)
            .Take(take)
            .ToListAsync();

        return Ok(new { items });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var r = await _db.Restaurants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.Status == RestaurantStatuses.Approved);
        if (r == null)
            return NotFound(new { message = "Không tìm thấy quán hoặc quán chưa hoạt động." });

        var foodCount = await _db.Foods.CountAsync(f => f.RestaurantId == id && f.IsAvailable);
        var categoryCount = await _db.Categories.CountAsync(c => c.RestaurantId == id);

        var reviewCount = 0;
        double? avgRating = null;
        try
        {
            reviewCount = await _db.RestaurantReviews.AsNoTracking().CountAsync(rv => rv.RestaurantId == id);
            avgRating = reviewCount == 0
                ? (double?)null
                : await _db.RestaurantReviews.AsNoTracking().Where(rv => rv.RestaurantId == id).AverageAsync(rv => (double)rv.Rating);
        }
        catch
        {
            reviewCount = 0;
            avgRating = null;
        }

        return Ok(new
        {
            r.Id,
            r.Name,
            r.Address,
            r.Phone,
            r.CoverImage,
            r.GalleryImage1,
            r.GalleryImage2,
            r.GalleryImage3,
            r.IsOnSale,
            r.SalePercent,
            foodCount,
            categoryCount,
            reviewCount,
            avgRating
        });
    }

    [HttpGet]
    public async Task<IActionResult> Reviews(int? restaurantId = null, int? id = null, int page = 1, int pageSize = 10, int? rating = null)
    {
        var targetRestaurantId = restaurantId ?? id;
        if (!targetRestaurantId.HasValue)
            return BadRequest(new { message = "Thiếu restaurantId." });

        try
        {
            var exists = await _db.Restaurants.AsNoTracking().AnyAsync(r => r.Id == targetRestaurantId.Value && r.Status == RestaurantStatuses.Approved);
            if (!exists)
                return NotFound(new { message = "Không tìm thấy quán." });

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 50);

            var query = _db.RestaurantReviews.AsNoTracking()
                .Where(r => r.RestaurantId == targetRestaurantId.Value);

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

            return Ok(new { restaurantId = targetRestaurantId.Value, page, pageSize, total, items });
        }
        catch
        {
            return Ok(new { restaurantId = targetRestaurantId.Value, page = Math.Max(1, page), pageSize = Math.Clamp(pageSize, 5, 50), total = 0, items = Array.Empty<object>() });
        }
    }
}
