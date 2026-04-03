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
public class UsersController : Controller
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? role = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = _db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.FullName,
                u.Email,
                u.Phone,
                u.Role,
                u.CreatedAt,
                RestaurantName = u.OwnedRestaurant != null ? u.OwnedRestaurant.Name : null,
                RestaurantStatus = u.OwnedRestaurant != null ? u.OwnedRestaurant.Status : null
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    public class UpdateRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }

    [HttpPut]
    public async Task<IActionResult> EditRole(int id, [FromBody] UpdateRoleRequest body)
    {
        if (!Roles.All.Contains(body.Role))
            return BadRequest(new { message = "Role không hợp lệ." });

        var user = await _db.Users.Include(u => u.OwnedRestaurant).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();

        if (user.Role == Roles.Admin && body.Role != Roles.Admin)
            return BadRequest(new { message = "Không hạ role Admin từ API này (an toàn)." });

        if (user.OwnedRestaurant != null && body.Role != Roles.Seller)
            return BadRequest(new { message = "User còn quán — chỉ có thể giữ role Seller hoặc xử lý quán trước." });

        user.Role = body.Role;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật role.", user.Id, user.Role });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.Include(u => u.OwnedRestaurant).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound();
        if (user.Role == Roles.Admin)
            return BadRequest(new { message = "Không xóa tài khoản Admin." });

        if (user.OwnedRestaurant != null)
            return BadRequest(new { message = "Seller còn quán — xóa/sửa quán trước hoặc chuyển chủ." });

        if (await _db.Orders.AnyAsync(o => o.UserId == id))
            return BadRequest(new { message = "User còn đơn hàng — không xóa." });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã xóa user." });
    }
}
