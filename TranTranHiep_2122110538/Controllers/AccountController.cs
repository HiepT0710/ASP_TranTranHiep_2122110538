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
    private readonly IWebHostEnvironment _env;
    private readonly IEmailService _emailService;

    public AccountController(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        IUserCartService userCart,
        IConfiguration config,
        IWebHostEnvironment env,
        IEmailService emailService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _userCart = userCart;
        _config = config;
        _env = env;
        _emailService = emailService;
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

        if (user.IsLocked)
            return Unauthorized(new { message = string.IsNullOrWhiteSpace(user.LockReason) ? "Tài khoản đã bị khóa." : $"Tài khoản đã bị khóa. Lý do: {user.LockReason}" });

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
            user = await BuildProfileResponse(user.Id, restaurantId)
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
    public async Task<IActionResult> Me()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await BuildProfileResponse(id));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (u == null)
            return NotFound(new { message = "Không tìm thấy tài khoản." });

        var restaurantId = u.Role == Roles.Seller
            ? await _db.Restaurants.AsNoTracking().Where(r => r.OwnerId == u.Id).Select(r => (int?)r.Id).FirstOrDefaultAsync()
            : null;

        return Ok(await BuildProfileResponse(u.Id, restaurantId));
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

        u.FullName = model.FullName.Trim();
        u.Email = model.Email.Trim();
        u.Phone = model.Phone?.Trim();
        u.Address = model.Address?.Trim();

        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật hồ sơ." });
    }

    [Authorize]
    [HttpPost]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Avatar([FromForm] UpdateAvatarRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var file = model.Avatar;
        if (file.Length == 0)
            return BadRequest(new { message = "Ảnh avatar không hợp lệ." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allow = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allow.Contains(ext))
            return BadRequest(new { message = "Chỉ hỗ trợ jpg, jpeg, png, webp." });

        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (u == null)
            return NotFound(new { message = "Không tìm thấy tài khoản." });

        var uploadDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "avatars");
        Directory.CreateDirectory(uploadDir);
        var fileName = $"avatar_{id}_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadDir, fileName);
        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        u.AvatarUrl = $"/uploads/avatars/{fileName}";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật avatar.", avatarUrl = u.AvatarUrl });
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
    [HttpPost]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == model.Email.Trim());
        if (user == null)
            return Ok(new { message = "Nếu email tồn tại, mã đặt lại mật khẩu đã được gửi." });

        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Email = user.Email ?? model.Email.Trim(),
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var frontendBaseUrl = _config["Frontend:BaseUrl"]?.TrimEnd('/');
        var resetUrl = !string.IsNullOrWhiteSpace(frontendBaseUrl)
            ? $"{frontendBaseUrl}/reset-password?email={Uri.EscapeDataString(model.Email.Trim())}&token={Uri.EscapeDataString(token)}"
            : $"{Request.Scheme}://{Request.Host}/reset-password?email={Uri.EscapeDataString(model.Email.Trim())}&token={Uri.EscapeDataString(token)}";
        await _emailService.SendAsync(model.Email.Trim(), "Đặt lại mật khẩu", $"<p>Bạn vừa yêu cầu đặt lại mật khẩu.</p><p>Vui lòng nhấn vào liên kết bên dưới để đặt lại mật khẩu.</p><p><a href='{resetUrl}'>Đặt lại mật khẩu</a></p>");

        return Ok(new { message = "Nếu email tồn tại, mã đặt lại mật khẩu đã được gửi." });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var resetToken = await _db.PasswordResetTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Email == model.Email.Trim() && x.Token == model.Token.Trim() && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow);

        if (resetToken?.User == null)
            return BadRequest(new { message = "Mã đặt lại không hợp lệ hoặc đã hết hạn." });

        resetToken.User.Password = _passwordHasher.HashPassword(resetToken.User, model.NewPassword);
        resetToken.UsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Đã đặt lại mật khẩu thành công." });
    }

    [Authorize]
    [HttpGet]
    public IActionResult WebPushPublicKey() =>
        Ok(new { publicKey = _config["WebPush:PublicKey"] ?? "" });

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

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() =>
        StatusCode(403, new { message = "Bạn không có quyền truy cập." });

    private async Task<object> BuildProfileResponse(int userId, int? restaurantId = null)
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (u == null)
            return new { };

        restaurantId ??= u.Role == Roles.Seller
            ? await _db.Restaurants.AsNoTracking().Where(r => r.OwnerId == u.Id).Select(r => (int?)r.Id).FirstOrDefaultAsync()
            : null;

        return new
        {
            id = u.Id,
            username = u.Username,
            role = u.Role,
            fullName = u.FullName,
            email = u.Email,
            phone = u.Phone,
            address = u.Address,
            avatarUrl = u.AvatarUrl,
            createdAt = u.CreatedAt,
            restaurantId,
            restaurantStatus = u.Role == Roles.Seller
                ? await _db.Restaurants.AsNoTracking().Where(r => r.OwnerId == u.Id).Select(r => r.Status).FirstOrDefaultAsync()
                : null
        };
    }
}
