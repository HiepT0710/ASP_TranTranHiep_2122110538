using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Infrastructure;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AccountController(AppDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _db.Users.AnyAsync(u => u.Username == model.Username))
            return Conflict(new { message = "Username đã tồn tại." });

        var user = new User
        {
            Username = model.Username,
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            Address = model.Address,
            Role = Roles.User,
            CreatedAt = DateTime.UtcNow
        };
        user.Password = _passwordHasher.HashPassword(user, model.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Đăng ký thành công.", userId = user.Id });
    }

    /// <summary>Đăng ký Seller: tạo tài khoản + quán (Pending), chờ Admin duyệt.</summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> RegisterSeller([FromBody] RegisterSellerRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _db.Users.AnyAsync(u => u.Username == model.Username))
            return Conflict(new { message = "Username đã tồn tại." });

        var user = new User
        {
            Username = model.Username,
            FullName = model.FullName,
            Email = model.Email,
            Phone = model.Phone,
            Address = model.Address,
            Role = Roles.Seller,
            CreatedAt = DateTime.UtcNow
        };
        user.Password = _passwordHasher.HashPassword(user, model.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var restaurant = new Restaurant
        {
            Name = model.RestaurantName.Trim(),
            OwnerId = user.Id,
            Address = model.Address,
            Phone = model.Phone,
            Status = RestaurantStatuses.Pending
        };
        _db.Restaurants.Add(restaurant);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Đăng ký seller thành công. Chờ Admin duyệt quán.",
            userId = user.Id,
            restaurantId = restaurant.Id,
            restaurantStatus = restaurant.Status
        });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
        if (user == null)
            return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });

        var verify = _passwordHasher.VerifyHashedPassword(user, user.Password, model.Password);
        if (verify == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };

        if (user.Role == Roles.Seller)
        {
            var rest = await _db.Restaurants.AsNoTracking().FirstOrDefaultAsync(r => r.OwnerId == user.Id);
            if (rest != null)
                claims.Add(new Claim(AuthClaims.RestaurantId, rest.Id.ToString()));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        int? restaurantId = user.Role == Roles.Seller
            ? await _db.Restaurants.AsNoTracking().Where(r => r.OwnerId == user.Id).Select(r => (int?)r.Id).FirstOrDefaultAsync()
            : null;

        return Ok(new
        {
            message = "Đăng nhập thành công.",
            user = new
            {
                user.Id,
                user.Username,
                user.Role,
                user.FullName,
                restaurantId,
                restaurantStatus = user.Role == Roles.Seller
                    ? await _db.Restaurants.AsNoTracking().Where(r => r.OwnerId == user.Id).Select(r => r.Status).FirstOrDefaultAsync()
                    : null
            }
        });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Đã đăng xuất." });
    }

    [Authorize]
    [HttpGet]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.Identity?.Name;
        var role = User.FindFirstValue(ClaimTypes.Role);
        var restaurantId = User.FindFirstValue(AuthClaims.RestaurantId);
        return Ok(new { id, name, role, restaurantId });
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() =>
        StatusCode(403, new { message = "Bạn không có quyền truy cập." });
}
