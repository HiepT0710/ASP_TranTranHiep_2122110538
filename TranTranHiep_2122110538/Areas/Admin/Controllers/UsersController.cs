using System.Security.Claims;
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
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, string? role = null, bool? locked = null, string? keyword = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = _db.Users.AsNoTracking().Include(u => u.OwnedRestaurant).AsQueryable();
        if (!string.IsNullOrWhiteSpace(role)) query = query.Where(u => u.Role == role);
        if (locked.HasValue) query = query.Where(u => u.IsLocked == locked.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(u => (u.Username ?? "").Contains(k) || (u.FullName ?? "").Contains(k) || (u.Email ?? "").Contains(k));
        }

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
                u.IsLocked,
                u.LockReason,
                u.CreatedAt,
                RestaurantId = u.OwnedRestaurant != null ? u.OwnedRestaurant.Id : (int?)null,
                RestaurantName = u.OwnedRestaurant != null ? u.OwnedRestaurant.Name : null,
                RestaurantStatus = u.OwnedRestaurant != null ? u.OwnedRestaurant.Status : null
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var user = await _db.Users.AsNoTracking().Include(u => u.OwnedRestaurant).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        var history = await _db.AuditLogs.AsNoTracking()
            .Where(x => x.EntityType == nameof(User) && x.EntityId == id)
            .OrderByDescending(x => x.Id)
            .Take(50)
            .Select(x => new { x.Id, x.Action, x.Note, x.MetadataJson, x.CreatedAt })
            .ToListAsync();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.FullName,
            user.Email,
            user.Phone,
            user.Address,
            user.Role,
            user.IsLocked,
            user.LockReason,
            user.CreatedAt,
            Restaurant = user.OwnedRestaurant == null ? null : new { user.OwnedRestaurant.Id, user.OwnedRestaurant.Name, user.OwnedRestaurant.Status },
            History = history
        });
    }

    public class UpdateRoleRequest { public string Role { get; set; } = string.Empty; }
    public class LockRequest { public string? Reason { get; set; } }

    [HttpPut]
    public async Task<IActionResult> EditRole(int id, [FromBody] UpdateRoleRequest body)
    {
        if (!Roles.All.Contains(body.Role)) return BadRequest(new { message = "Role không hợp lệ." });
        var user = await _db.Users.Include(u => u.OwnedRestaurant).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        if (user.Role == Roles.Admin && body.Role != Roles.Admin) return BadRequest(new { message = "Không hạ role Admin từ API này (an toàn)." });
        if (user.OwnedRestaurant != null && body.Role != Roles.Seller) return BadRequest(new { message = "User còn quán — chỉ có thể giữ role Seller hoặc xử lý quán trước." });
        user.Role = body.Role;
        _db.AuditLogs.Add(new AuditLog { ActorUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), Action = "USER_ROLE_CHANGED", EntityType = nameof(User), EntityId = user.Id, Note = $"Role -> {user.Role}" });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật role.", user.Id, user.Role });
    }

    [HttpPost]
    public async Task<IActionResult> ResetRole(int id)
    {
        var user = await _db.Users.Include(u => u.OwnedRestaurant).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        if (user.Role == Roles.Admin) return BadRequest(new { message = "Không reset role Admin." });
        user.Role = user.OwnedRestaurant != null ? Roles.Seller : Roles.User;
        _db.AuditLogs.Add(new AuditLog { ActorUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), Action = "USER_ROLE_RESET", EntityType = nameof(User), EntityId = user.Id, Note = $"Role reset -> {user.Role}" });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã reset role.", user.Id, user.Role });
    }

    [HttpPost]
    public async Task<IActionResult> Lock(int id, [FromBody] LockRequest body)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        if (user.Role == Roles.Admin) return BadRequest(new { message = "Không khóa Admin." });
        user.IsLocked = true;
        user.LockReason = body.Reason?.Trim();
        _db.AuditLogs.Add(new AuditLog { ActorUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), Action = "USER_LOCKED", EntityType = nameof(User), EntityId = user.Id, Note = user.LockReason });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã khóa tài khoản." });
    }

    [HttpPost]
    public async Task<IActionResult> Unlock(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        user.IsLocked = false;
        user.LockReason = null;
        _db.AuditLogs.Add(new AuditLog { ActorUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), Action = "USER_UNLOCKED", EntityType = nameof(User), EntityId = user.Id });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã mở khóa tài khoản." });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.Include(u => u.OwnedRestaurant).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        if (user.Role == Roles.Admin) return BadRequest(new { message = "Không xóa tài khoản Admin." });
        if (user.OwnedRestaurant != null) return BadRequest(new { message = "Seller còn quán — xóa/sửa quán trước hoặc chuyển chủ." });
        if (await _db.Orders.AnyAsync(o => o.UserId == id)) return BadRequest(new { message = "User còn đơn hàng — không xóa." });
        _db.Users.Remove(user);
        _db.AuditLogs.Add(new AuditLog { ActorUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), Action = "USER_DELETED", EntityType = nameof(User), EntityId = user.Id, Note = user.Username });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã xóa user." });
    }
}
