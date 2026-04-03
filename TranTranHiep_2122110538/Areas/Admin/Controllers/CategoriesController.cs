using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Areas.Admin.Controllers;

[Area("Admin")]
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("[area]/[controller]/[action]/{id?}")]
public class CategoriesController : Controller
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var list = await _db.Categories.AsNoTracking()
            .Include(c => c.Restaurant)
            .OrderBy(c => c.Restaurant!.Name).ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.RestaurantId,
                RestaurantName = c.Restaurant!.Name,
                FoodCount = c.Foods.Count
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var c = await _db.Categories.AsNoTracking().Include(x => x.Restaurant).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null)
            return NotFound();
        return Ok(new
        {
            c.Id,
            c.Name,
            c.Description,
            c.RestaurantId,
            RestaurantName = c.Restaurant?.Name
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Category model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest(new { message = "Tên danh mục bắt buộc." });

        if (!await _db.Restaurants.AnyAsync(r => r.Id == model.RestaurantId))
            return BadRequest(new { message = "Quán không tồn tại." });

        model.Id = 0;
        _db.Categories.Add(model);
        await _db.SaveChangesAsync();
        return Ok(new { model.Id, message = "Đã tạo." });
    }

    [HttpPut]
    public async Task<IActionResult> Edit(int id, [FromBody] Category model)
    {
        var entity = await _db.Categories.FindAsync(id);
        if (entity == null)
            return NotFound();

        if (!await _db.Restaurants.AnyAsync(r => r.Id == model.RestaurantId))
            return BadRequest(new { message = "Quán không tồn tại." });

        entity.Name = model.Name;
        entity.Description = model.Description;
        entity.RestaurantId = model.RestaurantId;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật." });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.Categories.Include(c => c.Foods).FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null)
            return NotFound();
        if (entity.Foods.Count > 0)
            return BadRequest(new { message = "Không xóa được: còn món trong danh mục." });

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã xóa." });
    }
}
