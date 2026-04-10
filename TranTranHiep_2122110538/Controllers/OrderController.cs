using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Infrastructure;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.Services;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Controllers;

[ApiController]
[Authorize]
[Route("[controller]/[action]/{id?}")]
public class OrderController : Controller
{
    private const string CartKey = "Cart";
    private readonly AppDbContext _db;
    private readonly IOrderAuditService _audit;
    private readonly IOrderNotificationService _notify;
    private readonly IUserCartService _userCart;
    private readonly IInventoryService _inventory;

    public OrderController(
        AppDbContext db,
        IOrderAuditService audit,
        IOrderNotificationService notify,
        IUserCartService userCart,
        IInventoryService inventory)
    {
        _db = db;
        _audit = audit;
        _notify = notify;
        _userCart = userCart;
        _inventory = inventory;
    }

    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest? body)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var cart = await _userCart.GetCartLinesAsync(HttpContext);
        if (cart.Count == 0)
            return BadRequest(new { message = "Giỏ hàng trống." });

        var paymentMethod = string.IsNullOrWhiteSpace(body?.PaymentMethod)
            ? PaymentMethods.COD
            : body!.PaymentMethod!.Trim();
        if (!PaymentMethods.All.Contains(paymentMethod))
            return BadRequest(new { message = "Phương thức thanh toán phải là COD, VNPay hoặc MoMo." });

        var ids = cart.Select(c => c.FoodId).ToList();
        var foods = await _db.Foods
            .Include(f => f.Restaurant)
            .Where(f => ids.Contains(f.Id) && f.IsAvailable)
            .ToDictionaryAsync(f => f.Id);

        decimal total = 0;
        var lines = new List<OrderDetail>();
        int? restaurantId = null;

        foreach (var item in cart)
        {
            if (!foods.TryGetValue(item.FoodId, out var food))
                return BadRequest(new { message = $"Món Id {item.FoodId} không hợp lệ." });

            if (food.StockQuantity < item.Quantity)
                return BadRequest(new { message = $"Không đủ tồn kho cho món \"{food.Name}\" (còn {food.StockQuantity})." });

            if (food.Restaurant!.Status != RestaurantStatuses.Approved)
                return BadRequest(new { message = $"Quán của món \"{food.Name}\" chưa được duyệt hoặc không hoạt động." });

            if (restaurantId == null)
                restaurantId = food.RestaurantId;
            else if (restaurantId != food.RestaurantId)
                return BadRequest(new { message = "Một đơn chỉ chứa món từ một quán. Hãy đặt từng quán riêng." });

            var lineTotal = food.Price * item.Quantity;
            total += lineTotal;
            lines.Add(new OrderDetail
            {
                FoodId = food.Id,
                Quantity = item.Quantity,
                Price = food.Price
            });
        }

        if (restaurantId == null)
            return BadRequest(new { message = "Không xác định được quán." });

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        await using var tx = await _db.Database.BeginTransactionAsync();
        var deduct = await _inventory.TryDeductStockForLinesAsync(cart);
        if (!deduct.Ok)
        {
            await tx.RollbackAsync();
            return BadRequest(new { message = deduct.ErrorMessage });
        }

        var order = new Order
        {
            UserId = userId,
            RestaurantId = restaurantId.Value,
            OrderDate = DateTime.UtcNow,
            TotalAmount = total,
            Status = OrderStatuses.Pending,
            Address = body?.Address ?? user?.Address,
            Phone = body?.Phone ?? user?.Phone,
            PaymentMethod = paymentMethod,
            PaymentStatus = PaymentStatuses.Pending
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        _audit.AddStatusChange(order.Id, null, OrderStatuses.Pending, userId, Roles.User, "Tạo đơn");

        foreach (var line in lines)
            line.OrderId = order.Id;

        _db.OrderDetails.AddRange(lines);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _userCart.ClearDatabaseCartAsync(userId);
        HttpContext.Session.SetJson(CartKey, new List<CartItemDto>());

        await _notify.BroadcastOrderAsync(order);
        _notify.LogEmailStub(
            $"Đơn hàng #{order.Id}",
            $"Khách đặt món, tổng {order.TotalAmount}, PT: {order.PaymentMethod}.");
        if (!string.IsNullOrWhiteSpace(order.Phone))
            _notify.LogSmsStub(order.Phone!, $"Đơn #{order.Id} đã gửi tới quán.");

        return Ok(new
        {
            message = "Đặt hàng thành công.",
            orderId = order.Id,
            total = order.TotalAmount,
            restaurantId = order.RestaurantId,
            paymentMethod = order.PaymentMethod,
            paymentStatus = order.PaymentStatus
        });
    }

    /// <summary>Giả lập thanh toán VNPay/MoMo (báo cáo — không gọi API thật).</summary>
    [HttpPost]
    public async Task<IActionResult> SimulateOnlinePayment(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn." });

        if (order.PaymentMethod != PaymentMethods.VNPay && order.PaymentMethod != PaymentMethods.MoMo)
            return BadRequest(new { message = "Chỉ áp dụng cho đơn thanh toán VNPay hoặc MoMo." });

        if (order.PaymentStatus != PaymentStatuses.Pending)
            return BadRequest(new { message = "Đơn đã thanh toán hoặc không còn chờ thanh toán." });

        if (order.Status == OrderStatuses.Cancelled)
            return BadRequest(new { message = "Đơn đã hủy." });

        order.PaymentStatus = PaymentStatuses.Paid;
        order.PaidAt = DateTime.UtcNow;
        var txn = "SIM-" + Guid.NewGuid().ToString("N")[..16];
        _db.OrderPayments.Add(new OrderPayment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            Kind = PaymentKinds.Online,
            Method = order.PaymentMethod,
            Status = PaymentStatuses.Paid,
            ExternalTransactionId = txn,
            Note = "Giả lập IPN/callback cổng thanh toán (demo)."
        });

        await _db.SaveChangesAsync();

        _notify.LogEmailStub($"Thanh toán thành công đơn #{order.Id}", $"Mã GD: {txn}");
        await _notify.BroadcastOrderAsync(order);

        return Ok(new { message = "Thanh toán thành công (giả lập).", transactionId = txn, order.Id, order.PaymentStatus });
    }

    [HttpGet]
    public async Task<IActionResult> MyOrders(int page = 1, int pageSize = 10)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.Orders.AsNoTracking()
            .Include(o => o.Restaurant)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new
            {
                o.Id,
                o.RestaurantId,
                RestaurantName = o.Restaurant!.Name,
                o.OrderDate,
                o.TotalAmount,
                o.Status,
                o.Address,
                o.Phone,
                o.PaymentMethod,
                o.PaymentStatus,
                o.PaidAt,
                o.TrackingNumber,
                o.ShipperName,
                o.RefundedAt,
                o.CancelledAt,
                o.CancelReason,
                canCancel = o.Status == OrderStatuses.Pending && o.PaymentStatus != PaymentStatuses.Paid
            })
            .ToListAsync();

        return Ok(new { page, pageSize, total, items });
    }

    public class CancelOrderRequest
    {
        public string? Reason { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelOrderRequest? body)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn." });

        if (!OrderStatuses.CustomerCancellable.Contains(order.Status))
            return BadRequest(new { message = "Chỉ hủy được khi đơn đang chờ quán xác nhận (chưa chuẩn bị)." });

        if (order.PaymentStatus == PaymentStatuses.Paid)
            return BadRequest(new { message = "Đã thanh toán — không hủy qua kênh này (liên hệ quán hoặc admin)." });

        var from = order.Status;
        order.Status = OrderStatuses.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelledBy = OrderCancelledBy.Customer;
        if (!string.IsNullOrWhiteSpace(body?.Reason))
        {
            var r = body.Reason.Trim();
            order.CancelReason = r.Length > 500 ? r[..500] : r;
        }

        _audit.AddStatusChange(order.Id, from, OrderStatuses.Cancelled, userId, Roles.User, order.CancelReason);

        await _db.SaveChangesAsync();

        if (InventoryRules.ShouldRestoreStockOnCancel(from, OrderStatuses.Cancelled))
            await _inventory.RestoreStockForOrderAsync(order.Id);

        await _notify.BroadcastOrderAsync(order);
        _notify.LogEmailStub($"Hủy đơn #{order.Id}", order.CancelReason ?? "");

        return Ok(new { message = "Đã hủy đơn.", order.Id, order.Status, order.CancelledAt });
    }

    public class ReviewLineRequest
    {
        public int FoodId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class SubmitReviewRequest
    {
        public int OrderId { get; set; }
        public List<ReviewLineRequest> Items { get; set; } = new();
    }

    /// <summary>Đánh giá món sau khi đơn Completed (một lần cho mỗi món trong đơn).</summary>
    [HttpPost]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest body)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (body.Items.Count == 0)
            return BadRequest(new { message = "Cần ít nhất một dòng đánh giá." });

        var order = await _db.Orders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == body.OrderId && o.UserId == userId);
        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn." });

        if (order.Status != OrderStatuses.Completed)
            return BadRequest(new { message = "Chỉ đánh giá sau khi đơn hoàn thành." });

        var foodIdsInOrder = order.OrderDetails.Select(d => d.FoodId).ToHashSet();
        foreach (var line in body.Items)
        {
            if (line.Rating is < 1 or > 5)
                return BadRequest(new { message = "Rating từ 1 đến 5." });
            if (!foodIdsInOrder.Contains(line.FoodId))
                return BadRequest(new { message = $"Món {line.FoodId} không thuộc đơn này." });
        }

        var existing = await _db.FoodReviews
            .Where(r => r.OrderId == order.Id)
            .Select(r => r.FoodId)
            .ToListAsync();
        var existingSet = existing.ToHashSet();
        foreach (var line in body.Items)
        {
            if (existingSet.Contains(line.FoodId))
                return BadRequest(new { message = $"Đã đánh giá món Id {line.FoodId}." });
        }

        foreach (var line in body.Items)
        {
            var comment = line.Comment?.Trim();
            if (!string.IsNullOrEmpty(comment) && comment.Length > 2000)
                comment = comment[..2000];

            _db.FoodReviews.Add(new FoodReview
            {
                OrderId = order.Id,
                FoodId = line.FoodId,
                UserId = userId,
                Rating = line.Rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã gửi đánh giá.", orderId = order.Id, count = body.Items.Count });
    }

    [HttpGet]
    public async Task<IActionResult> StatusHistory(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole(Roles.Admin);
        var isSeller = User.IsInRole(Roles.Seller);
        int? sellerRestaurantId = null;
        if (isSeller)
        {
            var rid = User.FindFirstValue(AuthClaims.RestaurantId);
            if (int.TryParse(rid, out var parsed))
                sellerRestaurantId = parsed;
            else
                sellerRestaurantId = await _db.Restaurants.AsNoTracking()
                    .Where(r => r.OwnerId == userId)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefaultAsync();
        }

        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn." });

        if (order.UserId != userId && !isAdmin && !(isSeller && sellerRestaurantId == order.RestaurantId))
            return StatusCode(403, new { message = "Không xem được lịch sử đơn này." });

        var items = await _db.OrderStatusHistories.AsNoTracking()
            .Where(h => h.OrderId == id)
            .OrderBy(h => h.CreatedAt)
            .Select(h => new
            {
                h.FromStatus,
                h.ToStatus,
                h.ActorUserId,
                h.ActorRole,
                h.Note,
                h.CreatedAt
            })
            .ToListAsync();

        return Ok(new { orderId = id, items });
    }

    /// <summary>Lịch sử chat theo đơn (khách / seller quán đó / admin).</summary>
    [HttpGet]
    public async Task<IActionResult> ChatMessages(int id, int page = 1, int pageSize = 40)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole(Roles.Admin);
        var isSeller = User.IsInRole(Roles.Seller);
        int? sellerRestaurantId = null;
        if (isSeller)
        {
            var rid = User.FindFirstValue(AuthClaims.RestaurantId);
            if (int.TryParse(rid, out var parsed))
                sellerRestaurantId = parsed;
            else
                sellerRestaurantId = await _db.Restaurants.AsNoTracking()
                    .Where(r => r.OwnerId == userId)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefaultAsync();
        }

        var order = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn." });

        if (order.UserId != userId && !isAdmin && !(isSeller && sellerRestaurantId == order.RestaurantId))
            return StatusCode(403, new { message = "Không xem được chat đơn này." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = _db.OrderMessages.AsNoTracking().Where(m => m.OrderId == id);
        var total = await query.CountAsync();
        var items = await query
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.UserId,
                Username = m.User!.Username,
                m.Message,
                m.CreatedAt
            })
            .ToListAsync();

        return Ok(new { orderId = id, page, pageSize, total, items });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole(Roles.Admin);
        var isSeller = User.IsInRole(Roles.Seller);
        int? sellerRestaurantId = null;
        if (isSeller)
        {
            var rid = User.FindFirstValue(AuthClaims.RestaurantId);
            if (int.TryParse(rid, out var parsed))
                sellerRestaurantId = parsed;
            else
                sellerRestaurantId = await _db.Restaurants.AsNoTracking()
                    .Where(r => r.OwnerId == userId)
                    .Select(r => (int?)r.Id)
                    .FirstOrDefaultAsync();
        }

        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Food)
            .Include(o => o.Restaurant)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn." });

        if (order.UserId != userId && !isAdmin && !(isSeller && sellerRestaurantId == order.RestaurantId))
            return StatusCode(403, new { message = "Không xem được đơn này." });

        var canCancel = order.UserId == userId && order.Status == OrderStatuses.Pending
            && order.PaymentStatus != PaymentStatuses.Paid;

        var reviewedFoodIds = await _db.FoodReviews.AsNoTracking()
            .Where(r => r.OrderId == id)
            .Select(r => r.FoodId)
            .ToListAsync();
        var reviewed = reviewedFoodIds.ToHashSet();
        var canSubmitReview = order.UserId == userId
            && order.Status == OrderStatuses.Completed
            && order.OrderDetails.Any(d => !reviewed.Contains(d.FoodId));

        return Ok(new
        {
            order.Id,
            order.RestaurantId,
            RestaurantName = order.Restaurant?.Name,
            order.OrderDate,
            order.TotalAmount,
            order.Status,
            order.Address,
            order.Phone,
            order.PaymentMethod,
            order.PaymentStatus,
            order.PaidAt,
            order.TrackingNumber,
            order.ShipperName,
            order.CancelledAt,
            order.CancelledBy,
            order.CancelReason,
            order.RefundedAt,
            order.RefundReason,
            canCancel,
            canSubmitReview,
            orderChatHubPath = "/hubs/orderchat",
            payments = order.Payments.OrderByDescending(p => p.CreatedAt).Select(p => new
            {
                p.Id,
                p.Amount,
                p.Kind,
                p.Method,
                p.Status,
                p.ExternalTransactionId,
                p.CreatedAt,
                p.Note
            }),
            details = order.OrderDetails.Select(od => new
            {
                od.FoodId,
                FoodName = od.Food!.Name,
                od.Quantity,
                od.Price,
                lineTotal = od.Price * od.Quantity,
                hasReview = reviewed.Contains(od.FoodId)
            })
        });
    }
}
