using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Areas.Seller.Controllers;

[Area("Seller")]
[ApiController]
[Authorize(Roles = Roles.Seller)]
[Route("[area]/[controller]/[action]/{id?}")]
public class CategoriesController : Controller
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
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
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán. Đăng ký seller trước." });

        var list = await _db.Categories.AsNoTracking()
            .Where(c => c.RestaurantId == rest.Id)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.Description, FoodCount = c.Foods.Count })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var c = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.RestaurantId == rest.Id);
        if (c == null)
            return NotFound();
        return Ok(new { c.Id, c.Name, c.Description });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Category model)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        if (string.IsNullOrWhiteSpace(model.Name))
            return BadRequest(new { message = "Tên danh mục bắt buộc." });

        model.Id = 0;
        model.RestaurantId = rest.Id;
        _db.Categories.Add(model);
        await _db.SaveChangesAsync();
        return Ok(new { model.Id, message = "Đã tạo." });
    }

    [HttpPut]
    public async Task<IActionResult> Edit(int id, [FromBody] Category model)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.RestaurantId == rest.Id);
        if (entity == null)
            return NotFound();

        entity.Name = model.Name;
        entity.Description = model.Description;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật." });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        var rest = await GetMyRestaurantAsync();
        if (rest == null)
            return BadRequest(new { message = "Chưa có quán." });

        var entity = await _db.Categories.Include(c => c.Foods).FirstOrDefaultAsync(c => c.Id == id && c.RestaurantId == rest.Id);
        if (entity == null)
            return NotFound();
        if (entity.Foods.Count > 0)
            return BadRequest(new { message = "Không xóa được: còn món trong danh mục." });

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã xóa." });
    }
}
