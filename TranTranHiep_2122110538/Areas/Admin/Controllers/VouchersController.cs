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
public class VouchersController : Controller
{
    private readonly AppDbContext _db;

    public VouchersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? promotionId = null)
    {
        var query = _db.Vouchers.AsNoTracking().Include(v => v.Promotion).AsQueryable();
        if (promotionId.HasValue)
            query = query.Where(v => v.PromotionId == promotionId.Value);

        var items = await query
            .OrderByDescending(v => v.Id)
            .Select(v => new
            {
                v.Id,
                v.Code,
                v.Note,
                v.IsActive,
                v.MinOrderAmount,
                v.MaxDiscountAmount,
                v.UsageLimit,
                v.UsedCount,
                v.PerUserLimit,
                v.StartAt,
                v.EndAt,
                v.PromotionId,
                PromotionName = v.Promotion!.Name,
                PromotionScope = v.Promotion!.Scope,
                v.Promotion!.DiscountPercent
            })
            .ToListAsync();

        return Ok(new { items });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var voucher = await _db.Vouchers.AsNoTracking().Include(v => v.Promotion).FirstOrDefaultAsync(v => v.Id == id);
        if (voucher == null)
            return NotFound();

        return Ok(new
        {
            voucher.Id,
            voucher.Code,
            voucher.Note,
            voucher.IsActive,
            voucher.MinOrderAmount,
            voucher.MaxDiscountAmount,
            voucher.UsageLimit,
            voucher.UsedCount,
            voucher.PerUserLimit,
            voucher.StartAt,
            voucher.EndAt,
            voucher.PromotionId,
            PromotionName = voucher.Promotion?.Name,
            PromotionScope = voucher.Promotion?.Scope,
            PromotionDiscountPercent = voucher.Promotion?.DiscountPercent
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VoucherUpsertRequest model)
    {
        if (!TryValidateVoucher(model, out var error))
            return BadRequest(new { message = error });

        var promotion = await _db.Promotions.FirstOrDefaultAsync(p => p.Id == model.PromotionId && p.IsActive);
        if (promotion == null)
            return BadRequest(new { message = "Promotion không tồn tại hoặc đang tắt." });

        var voucher = new Voucher
        {
            PromotionId = model.PromotionId,
            Code = model.Code.Trim().ToUpperInvariant(),
            Note = model.Note,
            MinOrderAmount = model.MinOrderAmount,
            MaxDiscountAmount = model.MaxDiscountAmount,
            UsageLimit = Math.Max(1, model.UsageLimit),
            PerUserLimit = Math.Max(1, model.PerUserLimit),
            UsedCount = 0,
            StartAt = model.StartAt ?? promotion.StartAt,
            EndAt = model.EndAt ?? promotion.EndAt,
            IsActive = model.IsActive
        };

        _db.Vouchers.Add(voucher);
        await _db.SaveChangesAsync();
        return Ok(new { voucher.Id, message = "Đã tạo voucher." });
    }

    [HttpPut]
    public async Task<IActionResult> Edit(int id, [FromBody] VoucherUpsertRequest model)
    {
        if (!TryValidateVoucher(model, out var error))
            return BadRequest(new { message = error });

        var voucher = await _db.Vouchers.FirstOrDefaultAsync(v => v.Id == id);
        if (voucher == null)
            return NotFound();

        var promotion = await _db.Promotions.FirstOrDefaultAsync(p => p.Id == model.PromotionId && p.IsActive);
        if (promotion == null)
            return BadRequest(new { message = "Promotion không tồn tại hoặc đang tắt." });

        voucher.PromotionId = model.PromotionId;
        voucher.Code = model.Code.Trim().ToUpperInvariant();
        voucher.Note = model.Note;
        voucher.MinOrderAmount = model.MinOrderAmount;
        voucher.MaxDiscountAmount = model.MaxDiscountAmount;
        voucher.UsageLimit = Math.Max(1, model.UsageLimit);
        voucher.PerUserLimit = Math.Max(1, model.PerUserLimit);
        voucher.StartAt = model.StartAt ?? voucher.StartAt;
        voucher.EndAt = model.EndAt ?? voucher.EndAt;
        voucher.IsActive = model.IsActive;

        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật voucher." });
    }

    [HttpPut]
    public async Task<IActionResult> Toggle(int id)
    {
        var voucher = await _db.Vouchers.FirstOrDefaultAsync(x => x.Id == id);
        if (voucher == null)
            return NotFound();

        voucher.IsActive = !voucher.IsActive;
        await _db.SaveChangesAsync();
        return Ok(new { message = voucher.IsActive ? "Đã bật voucher." : "Đã tắt voucher.", voucher.IsActive });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        var voucher = await _db.Vouchers.FirstOrDefaultAsync(x => x.Id == id);
        if (voucher == null)
            return NotFound();

        _db.Vouchers.Remove(voucher);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã xóa voucher." });
    }

    private bool TryValidateVoucher(VoucherUpsertRequest model, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(model.Code))
        {
            error = "Mã voucher bắt buộc.";
            return false;
        }

        if (model.UsageLimit < 1)
        {
            error = "Số lượt dùng phải >= 1.";
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
