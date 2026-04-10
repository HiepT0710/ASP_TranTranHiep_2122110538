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
using TranTranHiep_2122110538.Services;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUserCartService _userCart;
    private readonly IConfiguration _config;

    public AccountController(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        IUserCartService userCart,
        IConfiguration config)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _userCart = userCart;
        _config = config;
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

        await _userCart.MergeSessionIntoDatabaseAsync(HttpContext, user.Id);

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

    /// <summary>Thông tin hồ sơ đầy đủ (không có mật khẩu).</summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (u == null)
            return NotFound(new { message = "Không tìm thấy tài khoản." });

        return Ok(new
        {
            u.Id,
            u.Username,
            u.FullName,
            u.Email,
            u.Phone,
            u.Address,
            u.Role,
            u.CreatedAt
        });
    }

    [Authorize]
    [HttpPut]
    public async Task<IActionResult> Profile([FromBody] UpdateProfileRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u == null)
            return NotFound(new { message = "Không tìm thấy tài khoản." });

        if (!string.IsNullOrWhiteSpace(model.FullName))
            u.FullName = model.FullName.Trim();
        u.Email = string.IsNullOrWhiteSpace(model.Email) ? u.Email : model.Email.Trim();
        u.Phone = model.Phone?.Trim();
        u.Address = model.Address?.Trim();

        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật hồ sơ.", u.Id, u.FullName, u.Email, u.Phone, u.Address });
    }

    /// <summary>Public key VAPID cho trình duyệt đăng ký Web Push (để nhận thông báo nền).</summary>
    [Authorize]
    [HttpGet]
    public IActionResult WebPushPublicKey() =>
        Ok(new { publicKey = _config["WebPush:PublicKey"] ?? "" });

    /// <summary>Lưu subscription Web Push (một user có nhiều thiết bị).</summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> RegisterPush([FromBody] PushSubscriptionRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.Endpoint) || string.IsNullOrWhiteSpace(body.P256dh) ||
            string.IsNullOrWhiteSpace(body.Auth))
            return BadRequest(new { message = "Thiếu endpoint / keys." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ep = body.Endpoint.Trim();
        var existing = await _db.PushSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == ep);
        if (existing != null)
        {
            existing.P256dh = body.P256dh.Trim();
            existing.Auth = body.Auth.Trim();
        }
        else
        {
            _db.PushSubscriptions.Add(new PushSubscription
            {
                UserId = userId,
                Endpoint = ep.Length > 2048 ? ep[..2048] : ep,
                P256dh = body.P256dh.Trim(),
                Auth = body.Auth.Trim(),
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã đăng ký push." });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> UnregisterPush([FromBody] PushSubscriptionRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.Endpoint))
            return BadRequest(new { message = "Cần endpoint." });

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ep = body.Endpoint.Trim();
        await _db.PushSubscriptions.Where(s => s.UserId == userId && s.Endpoint == ep).ExecuteDeleteAsync();
        return Ok(new { message = "Đã gỡ đăng ký push." });
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u == null)
            return NotFound(new { message = "Không tìm thấy tài khoản." });

        if (_passwordHasher.VerifyHashedPassword(u, u.Password, model.CurrentPassword) == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });

        u.Password = _passwordHasher.HashPassword(u, model.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã đổi mật khẩu." });
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() =>
        StatusCode(403, new { message = "Bạn không có quyền truy cập." });
}
