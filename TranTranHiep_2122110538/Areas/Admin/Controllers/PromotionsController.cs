using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Areas.Admin.Controllers;

[Area("Admin")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("[area]/[controller]/[action]/{id?}")]
public class PromotionsController : Controller
{
    private readonly AppDbContext _db;

    public PromotionsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var items = await _db.Promotions.AsNoTracking()
            .Include(p => p.Vouchers)
            .Include(p => p.Restaurant)
            .Include(p => p.Food)
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
                RestaurantName = p.Restaurant != null ? p.Restaurant.Name : null,
                p.FoodId,
                FoodName = p.Food != null ? p.Food.Name : null,
                VoucherCount = p.Vouchers.Count
            })
            .ToListAsync();

        return Ok(new { items });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var promotion = await _db.Promotions.AsNoTracking()
            .Include(p => p.Restaurant)
            .Include(p => p.Food)
            .Include(p => p.Vouchers)
            .FirstOrDefaultAsync(p => p.Id == id);
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
            RestaurantName = promotion.Restaurant?.Name,
            promotion.FoodId,
            FoodName = promotion.Food?.Name,
            Vouchers = promotion.Vouchers.Select(v => new
            {
                v.Id,
                v.Code,
                v.IsActive,
                v.MinOrderAmount,
                v.MaxDiscountAmount,
                v.UsageLimit,
                v.UsedCount,
                v.StartAt,
                v.EndAt
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PromotionUpsertRequest model)
    {
        if (!TryValidatePromotion(model, out var error))
            return BadRequest(new { message = error });

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
        return Ok(new { promotion.Id, message = "Đã tạo chương trình khuyến mãi." });
    }

    [HttpPut]
    public async Task<IActionResult> Edit(int id, [FromBody] PromotionUpsertRequest model)
    {
        if (!TryValidatePromotion(model, out var error))
            return BadRequest(new { message = error });

        var promotion = await _db.Promotions.FirstOrDefaultAsync(p => p.Id == id);
        if (promotion == null)
            return NotFound();

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
        return Ok(new { message = "Đã cập nhật chương trình khuyến mãi." });
    }

    [HttpPut]
    public async Task<IActionResult> Toggle(int id)
    {
        var promotion = await _db.Promotions.FirstOrDefaultAsync(x => x.Id == id);
        if (promotion == null)
            return NotFound();

        promotion.IsActive = !promotion.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = promotion.IsActive ? "Đã bật khuyến mãi." : "Đã tắt khuyến mãi.", promotion.IsActive });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        var promotion = await _db.Promotions.Include(p => p.Vouchers).FirstOrDefaultAsync(x => x.Id == id);
        if (promotion == null)
            return NotFound();

        if (promotion.Vouchers.Count > 0)
            _db.Vouchers.RemoveRange(promotion.Vouchers);

        _db.Promotions.Remove(promotion);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã xóa chương trình khuyến mãi." });
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

        if (model.Scope == PromotionScopes.Restaurant && (model.RestaurantId == null || model.RestaurantId <= 0))
        {
            error = "Cần chọn quán cho promotion quán.";
            return false;
        }

        if (model.Scope == PromotionScopes.Food && (model.FoodId == null || model.FoodId <= 0))
        {
            error = "Cần chọn món cho promotion món.";
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
