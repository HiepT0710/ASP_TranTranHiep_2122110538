using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Areas.Seller.Controllers;

[Area("Seller")]
[ApiController]
[Authorize(Roles = Roles.Seller)]
[Route("[area]/[controller]/[action]/{id?}")]
public class PromotionsController : Controller
{
    private readonly AppDbContext _db;

    public PromotionsController(AppDbContext db)
    {
        _db = db;
    }

    private async Task<Restaurant?> GetMyRestaurantAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await _db.Restaurants.FirstOrDefaultAsync(r => r.OwnerId == userId);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null) return BadRequest(new { message = "Chưa có quán." });

        var items = await _db.Promotions.AsNoTracking()
            .Include(p => p.Vouchers)
            .Where(p => p.RestaurantId == rest.Id || p.Food!.RestaurantId == rest.Id)
            .OrderByDescending(p => p.Id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Description,
                p.Scope,
                p.IsActive,
                p.DiscountPercent,
                p.StartAt,
                p.EndAt,
                p.RestaurantId,
                p.FoodId,
                VoucherCount = p.Vouchers.Count
            })
            .ToListAsync();

        return Ok(new { items });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null) return BadRequest(new { message = "Chưa có quán." });

        var promotion = await _db.Promotions.AsNoTracking()
            .Include(p => p.Food)
            .Include(p => p.Vouchers)
            .FirstOrDefaultAsync(p => p.Id == id && (p.RestaurantId == rest.Id || p.Food!.RestaurantId == rest.Id));
        if (promotion == null)
            return NotFound();

        return Ok(new
        {
            promotion.Id,
            promotion.Name,
            promotion.Description,
            promotion.Scope,
            promotion.IsActive,
            promotion.DiscountPercent,
            promotion.StartAt,
            promotion.EndAt,
            promotion.RestaurantId,
            promotion.FoodId,
            Vouchers = promotion.Vouchers.Select(v => new { v.Id, v.Code, v.IsActive, v.UsedCount, v.UsageLimit })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PromotionUpsertRequest model)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null) return BadRequest(new { message = "Chưa có quán." });

        if (!TryValidatePromotion(model, out var error))
            return BadRequest(new { message = error });

        if (model.Scope == PromotionScopes.Restaurant && model.RestaurantId != rest.Id)
            return BadRequest(new { message = "Promotion quán phải thuộc quán của bạn." });

        if (model.Scope == PromotionScopes.Food)
        {
            if (model.FoodId == null)
                return BadRequest(new { message = "Cần chọn món." });
            var food = await _db.Foods.FirstOrDefaultAsync(f => f.Id == model.FoodId && f.RestaurantId == rest.Id);
            if (food == null) return BadRequest(new { message = "Món không thuộc quán của bạn." });
        }

        var promotion = new Promotion
        {
            Name = model.Name.Trim(),
            Description = model.Description,
            Scope = model.Scope,
            RestaurantId = model.RestaurantId,
            FoodId = model.FoodId,
            DiscountPercent = model.DiscountPercent,
            StartAt = model.StartAt ?? DateTime.UtcNow,
            EndAt = model.EndAt ?? DateTime.UtcNow.AddDays(30),
            IsActive = model.IsActive
        };
        _db.Promotions.Add(promotion);
        await _db.SaveChangesAsync();
        return Ok(new { promotion.Id, message = "Đã tạo promotion." });
    }

    [HttpPut]
    public async Task<IActionResult> Edit(int id, [FromBody] PromotionUpsertRequest model)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null) return BadRequest(new { message = "Chưa có quán." });

        if (!TryValidatePromotion(model, out var error))
            return BadRequest(new { message = error });

        var promotion = await _db.Promotions.FirstOrDefaultAsync(p => p.Id == id && (p.RestaurantId == rest.Id || p.Food!.RestaurantId == rest.Id));
        if (promotion == null)
            return NotFound();

        if (model.Scope == PromotionScopes.Restaurant && model.RestaurantId != rest.Id)
            return BadRequest(new { message = "Promotion quán phải thuộc quán của bạn." });

        if (model.Scope == PromotionScopes.Food)
        {
            if (model.FoodId == null)
                return BadRequest(new { message = "Cần chọn món." });
            var food = await _db.Foods.FirstOrDefaultAsync(f => f.Id == model.FoodId && f.RestaurantId == rest.Id);
            if (food == null) return BadRequest(new { message = "Món không thuộc quán của bạn." });
        }

        promotion.Name = model.Name.Trim();
        promotion.Description = model.Description;
        promotion.Scope = model.Scope;
        promotion.RestaurantId = model.RestaurantId;
        promotion.FoodId = model.FoodId;
        promotion.DiscountPercent = model.DiscountPercent;
        promotion.StartAt = model.StartAt ?? promotion.StartAt;
        promotion.EndAt = model.EndAt ?? promotion.EndAt;
        promotion.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật promotion." });
    }

    [HttpPut]
    public async Task<IActionResult> Toggle(int id)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null) return BadRequest(new { message = "Chưa có quán." });

        var promotion = await _db.Promotions.FirstOrDefaultAsync(p => p.Id == id && (p.RestaurantId == rest.Id || p.Food!.RestaurantId == rest.Id));
        if (promotion == null)
            return NotFound();

        promotion.IsActive = !promotion.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = promotion.IsActive ? "Đã bật khuyến mãi." : "Đã tắt khuyến mãi.", promotion.IsActive });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null) return BadRequest(new { message = "Chưa có quán." });

        var promotion = await _db.Promotions.Include(p => p.Vouchers).FirstOrDefaultAsync(p => p.Id == id && (p.RestaurantId == rest.Id || p.Food!.RestaurantId == rest.Id));
        if (promotion == null)
            return NotFound();

        if (promotion.Vouchers.Count > 0)
            _db.Vouchers.RemoveRange(promotion.Vouchers);

        _db.Promotions.Remove(promotion);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã xóa promotion." });
    }

    private bool TryValidatePromotion(PromotionUpsertRequest model, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            error = "Tên chương trình bắt buộc.";
            return false;
        }
        if (model.DiscountPercent is < 1 or > 100)
        {
            error = "Phần trăm giảm phải từ 1 đến 100.";
            return false;
        }
        if (model.Scope != PromotionScopes.Restaurant && model.Scope != PromotionScopes.Food)
        {
            error = "Scope không hợp lệ.";
            return false;
        }
        if ((model.EndAt ?? DateTime.UtcNow.AddDays(30)) <= (model.StartAt ?? DateTime.UtcNow))
        {
            error = "Ngày kết thúc phải sau ngày bắt đầu.";
            return false;
        }
        return true;
    }
}
