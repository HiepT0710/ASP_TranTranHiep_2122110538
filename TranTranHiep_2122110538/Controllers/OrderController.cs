using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Infrastructure;
using TranTranHiep_2122110538.Hubs;
using TranTranHiep_2122110538.Models;
using InventoryRules = TranTranHiep_2122110538.Services.InventoryRules;
using TranTranHiep_2122110538.Services;
using TranTranHiep_2122110538.ViewModels;

namespace TranTranHiep_2122110538.Controllers;

[ApiController]
[Authorize]
[Route("[controller]/[action]/{id?}")]
public class OrderController : Controller
{
    private const string CartKey = "Cart";
    private const string VnpayTmnCode = "DEMO12345";
    private const string VnpayHashSecret = "DEMO_HASH_SECRET_2026";
    private const string VnpayBaseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    private readonly AppDbContext _db;
    private readonly IOrderAuditService _audit;
    private readonly IOrderNotificationService _notify;
    private readonly IUserCartService _userCart;
    private readonly IInventoryService _inventory;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        AppDbContext db,
        IOrderAuditService audit,
        IOrderNotificationService notify,
        IUserCartService userCart,
        IInventoryService inventory,
        ILogger<OrderController> logger)
    {
        _db = db;
        _audit = audit;
        _notify = notify;
        _userCart = userCart;
        _inventory = inventory;
        _logger = logger;
    }

    private static string VnpayAmountVnd(decimal amount) => ((long)Math.Round(amount, 0)).ToString(CultureInfo.InvariantCulture);

    private static string BuildVnpaySecureHash(Dictionary<string, string> data)
    {
        var sorted = data
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}")
            .ToArray();
        var raw = string.Join('&', sorted);
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(VnpayHashSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private decimal GetSettingDecimal(string key, decimal fallback)
    {
        var raw = _db.SystemSettings.AsNoTracking().Where(s => s.Key == key).Select(s => s.Value).FirstOrDefault();
        return decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    }

    private int GetSettingInt(string key, int fallback)
    {
        var raw = _db.SystemSettings.AsNoTracking().Where(s => s.Key == key).Select(s => s.Value).FirstOrDefault();
        return int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    }

    private bool CanCancelOrder(Order order)
    {
        if (order.Status != OrderStatuses.Pending)
            return false;
        if (order.PaymentStatus == PaymentStatuses.Paid)
            return false;

        var cancelWindowMinutes = GetSettingInt("Order:CancelWindowMinutes", 10);
        var deadline = order.OrderDate.AddMinutes(cancelWindowMinutes);
        return DateTime.UtcNow <= deadline;
    }

    private DateTime GetCancelDeadline(DateTime orderDate)
    {
        var cancelWindowMinutes = GetSettingInt("Order:CancelWindowMinutes", 10);
        return orderDate.AddMinutes(cancelWindowMinutes);
    }

    private string BuildVnpayPaymentUrl(Order order, string returnUrl)
    {
        var txnRef = order.Id.ToString(CultureInfo.InvariantCulture);
        var amount = VnpayAmountVnd(order.TotalAmount) + "00";
        var ipAddr = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var createDate = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var expireDate = DateTime.UtcNow.AddMinutes(15).ToString("yyyyMMddHHmmss");
        var data = new Dictionary<string, string>
        {
            ["vnp_Amount"] = amount,
            ["vnp_BankCode"] = "",
            ["vnp_Command"] = "pay",
            ["vnp_CreateDate"] = createDate,
            ["vnp_CurrCode"] = "VND",
            ["vnp_IpAddr"] = ipAddr,
            ["vnp_Locale"] = "vn",
            ["vnp_OrderInfo"] = $"Thanh toan don hang #{order.Id}",
            ["vnp_OrderType"] = "other",
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TmnCode"] = VnpayTmnCode,
            ["vnp_TxnRef"] = txnRef,
            ["vnp_Version"] = "2.1.0",
            ["vnp_ExpireDate"] = expireDate
        };
        var query = string.Join('&', data
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var secureHash = BuildVnpaySecureHash(data);
        return $"{VnpayBaseUrl}?{query}&vnp_SecureHash={secureHash}";
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> SystemSettings()
    {
        var items = await _db.SystemSettings.AsNoTracking()
            .Where(s => s.Key == "Shipping:DefaultFee" || s.Key == "Shipping:FreeShipThreshold" || s.Key == "Order:CancelWindowMinutes")
            .Select(s => new { s.Key, s.Value })
            .ToListAsync();
        return Ok(new { items });
    }

    [HttpGet]
    public async Task<IActionResult> VoucherSuggestions()
    {
        var cart = await _userCart.GetCartLinesAsync(HttpContext);
        if (cart.Count == 0)
            return Ok(new { items = Array.Empty<object>() });

        var ids = cart.Select(c => c.FoodId).ToList();
        var foods = await _db.Foods
            .Include(f => f.Restaurant)
            .Where(f => ids.Contains(f.Id) && f.IsAvailable)
            .ToListAsync();

        if (foods.Count == 0)
            return Ok(new { items = Array.Empty<object>() });

        var restaurantId = foods.First().RestaurantId;
        var total = foods.Sum(f => f.Price * cart.First(c => c.FoodId == f.Id).Quantity);
        var foodIds = foods.Select(f => f.Id).ToHashSet();

        var now = DateTime.Now;
        var vouchers = await _db.Vouchers
            .Include(v => v.Promotion)
            .Where(v => v.IsActive)
            .OrderByDescending(v => v.UsedCount)
            .ThenBy(v => v.Code)
            .ToListAsync();

        var items = new List<object>();
        foreach (var v in vouchers)
        {
            if (v.Promotion == null)
                continue;

            var scopeOk = v.Promotion.Scope == PromotionScopes.Restaurant
                ? v.Promotion.RestaurantId == restaurantId
                : v.Promotion.Scope == PromotionScopes.Food
                    ? v.Promotion.FoodId.HasValue && foodIds.Contains(v.Promotion.FoodId.Value)
                    : false;

            if (!scopeOk)
                continue;

            var reasons = new List<string>();
            var canUse = true;

            if (v.StartAt > now)
            {
                canUse = false;
                reasons.Add($"Voucher chỉ dùng từ {v.StartAt:dd/MM/yyyy HH:mm}");
            }
            if (v.EndAt < now)
            {
                canUse = false;
                reasons.Add("Voucher đã hết hạn");
            }
            if (v.UsedCount >= v.UsageLimit)
            {
                canUse = false;
                reasons.Add("Voucher đã hết lượt sử dụng");
            }
            if (v.Promotion.StartAt > now)
            {
                canUse = false;
                reasons.Add($"Khuyến mãi áp dụng từ {v.Promotion.StartAt:dd/MM/yyyy HH:mm}");
            }
            if (v.Promotion.EndAt < now)
            {
                canUse = false;
                reasons.Add("Khuyến mãi đã kết thúc");
            }

            var remainingAmount = 0m;
            if (v.MinOrderAmount.HasValue && total < v.MinOrderAmount.Value)
            {
                canUse = false;
                remainingAmount = v.MinOrderAmount.Value - total;
                reasons.Add($"Cần thêm {remainingAmount:N0} để đạt đơn tối thiểu {v.MinOrderAmount.Value:N0}");
            }

            var estimatedDiscount = 0m;
            if (v.Promotion != null)
            {
                estimatedDiscount = total * v.Promotion.DiscountPercent / 100m;
                if (v.MaxDiscountAmount.HasValue)
                    estimatedDiscount = Math.Min(estimatedDiscount, v.MaxDiscountAmount.Value);
            }

            items.Add(new
            {
                v.Id,
                v.Code,
                v.Note,
                v.IsActive,
                PromotionName = v.Promotion.Name,
                PromotionScope = v.Promotion.Scope,
                v.Promotion.DiscountPercent,
                v.MinOrderAmount,
                v.MaxDiscountAmount,
                v.UsageLimit,
                v.UsedCount,
                v.StartAt,
                v.EndAt,
                canUse,
                statusText = canUse ? "Có thể sử dụng" : "Chưa đủ điều kiện sử dụng",
                reasons,
                requiredMinOrderAmount = v.MinOrderAmount,
                remainingAmount,
                estimatedDiscount
            });
        }

        return Ok(new { total, items });
    }

    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutPayload? body)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var cart = await _userCart.GetCartLinesAsync(HttpContext);
        if (cart.Count == 0)
            return BadRequest(new { message = "Giỏ hàng trống." });

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

        decimal discountAmount = 0;
        Voucher? appliedVoucher = null;
        if (!string.IsNullOrWhiteSpace(body?.VoucherCode))
        {
            var voucherCode = body.VoucherCode.Trim().ToUpperInvariant();
            appliedVoucher = await _db.Vouchers
                .Include(v => v.Promotion)
                .FirstOrDefaultAsync(v => v.Code == voucherCode && v.IsActive);
            if (appliedVoucher == null)
                return BadRequest(new { message = "Voucher không hợp lệ." });

            var now = DateTime.UtcNow;
            if (appliedVoucher.StartAt > now || appliedVoucher.EndAt < now)
                return BadRequest(new { message = "Voucher đã hết hạn hoặc chưa tới thời gian sử dụng." });
            if (appliedVoucher.UsedCount >= appliedVoucher.UsageLimit)
                return BadRequest(new { message = "Voucher đã hết lượt dùng." });
            if (appliedVoucher.MinOrderAmount.HasValue && total < appliedVoucher.MinOrderAmount.Value)
                return BadRequest(new { message = "Đơn hàng chưa đạt mức tối thiểu để dùng voucher." });
            if (appliedVoucher.Promotion == null)
                return BadRequest(new { message = "Voucher chưa gắn chương trình khuyến mãi." });
            if (!appliedVoucher.Promotion.IsActive)
                return BadRequest(new { message = "Chương trình khuyến mãi của voucher đang tắt." });
            if (appliedVoucher.Promotion.StartAt > now || appliedVoucher.Promotion.EndAt < now)
                return BadRequest(new { message = "Chương trình khuyến mãi của voucher không còn hiệu lực." });
            if (appliedVoucher.PerUserLimit <= 0)
                return BadRequest(new { message = "Voucher chưa cấu hình giới hạn sử dụng theo người dùng." });

            var usedByUser = await _db.Orders.AsNoTracking()
                .CountAsync(o => o.UserId == userId && o.VoucherCode == appliedVoucher.Code && o.PaymentStatus == PaymentStatuses.Paid);
            if (usedByUser >= appliedVoucher.PerUserLimit)
                return BadRequest(new { message = "Bạn đã dùng voucher này hết số lượt cho phép." });

            var targetRestaurantId = restaurantId.Value;
            var scopeOk = appliedVoucher.Promotion.Scope == PromotionScopes.Restaurant
                ? appliedVoucher.Promotion.RestaurantId == targetRestaurantId
                : appliedVoucher.Promotion.Scope == PromotionScopes.Food
                    ? appliedVoucher.Promotion.FoodId.HasValue && ids.Contains(appliedVoucher.Promotion.FoodId.Value)
                    : false;
            if (!scopeOk)
                return BadRequest(new { message = "Voucher không áp dụng cho giỏ hàng hiện tại." });
        }

        if (appliedVoucher?.Promotion != null)
        {
            discountAmount = total * appliedVoucher.Promotion.DiscountPercent / 100m;
            if (appliedVoucher.MaxDiscountAmount.HasValue)
                discountAmount = Math.Min(discountAmount, appliedVoucher.MaxDiscountAmount.Value);
            discountAmount = Math.Max(0, discountAmount);
        }

        var afterDiscountTotal = Math.Max(0, total - discountAmount);
        var shippingFee = GetSettingDecimal("Shipping:DefaultFee", 20000m);
        var freeShipThreshold = GetSettingDecimal("Shipping:FreeShipThreshold", 100000m);
        if (afterDiscountTotal >= freeShipThreshold)
            shippingFee = 0m;

        var grandTotal = Math.Max(0, afterDiscountTotal + shippingFee);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);

        await using var tx = await _db.Database.BeginTransactionAsync();
        var deduct = await _inventory.TryDeductStockForLinesAsync(cart);
        if (!deduct.Ok)
        {
            await tx.RollbackAsync();
            return BadRequest(new { message = deduct.ErrorMessage });
        }

        var paymentMethod = string.IsNullOrWhiteSpace(body?.PaymentMethod)
            ? PaymentMethods.COD
            : body!.PaymentMethod!.Trim();
        if (!PaymentMethods.All.Contains(paymentMethod))
            return BadRequest(new { message = "Phương thức thanh toán phải là COD, VNPay hoặc MoMo." });

        var order = new Order
        {
            UserId = userId,
            RestaurantId = restaurantId.Value,
            OrderDate = DateTime.UtcNow,
            TotalAmount = grandTotal,
            Status = OrderStatuses.Pending,
            Address = body?.Address ?? user?.Address,
            Phone = body?.Phone ?? user?.Phone,
            PaymentMethod = paymentMethod,
            PaymentStatus = PaymentStatuses.Pending,
            PaymentSource = "Manual",
            VoucherCode = appliedVoucher?.Code
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        _audit.AddStatusChange(order.Id, null, OrderStatuses.Pending, userId, Roles.User, "Tạo đơn");

        foreach (var line in lines)
            line.OrderId = order.Id;

        _db.OrderDetails.AddRange(lines);
        await _db.SaveChangesAsync();

        if (appliedVoucher != null)
        {
            appliedVoucher.UsedCount += 1;
            _db.Vouchers.Update(appliedVoucher);
            await _db.SaveChangesAsync();
        }

        order.TotalAmount = grandTotal;
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

        var result = new Dictionary<string, object?>
        {
            ["message"] = order.PaymentMethod == PaymentMethods.VNPay
                ? "Đã tạo đơn, chuyển sang trang thanh toán VNPay sandbox."
                : "Đặt hàng thành công.",
            ["orderId"] = order.Id,
            ["total"] = order.TotalAmount,
            ["restaurantId"] = order.RestaurantId,
            ["paymentMethod"] = order.PaymentMethod,
            ["paymentStatus"] = order.PaymentStatus
        };

        if (order.PaymentMethod == PaymentMethods.VNPay)
        {
            var returnUrl = Url.Action(nameof(VnpayReturn), "Order", new { id = order.Id }, Request.Scheme) ?? "/Order/VnpayReturn";
            result["paymentUrl"] = BuildVnpayPaymentUrl(order, returnUrl);
        }

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> VnpayReturn(int id, string? vnp_ResponseCode = null, string? vnp_TransactionStatus = null, string? vnp_TxnRef = null)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
        if (order == null)
            return NotFound(new { message = "Không tìm thấy đơn." });

        if (order.Status == OrderStatuses.Cancelled)
            return BadRequest(new { message = "Đơn đã bị hủy nên không thể thanh toán nữa." });

        if (order.Status == OrderStatuses.Completed)
            return BadRequest(new { message = "Đơn đã hoàn thành nên không thể thanh toán lại." });

        if (order.PaymentMethod != PaymentMethods.VNPay)
            return BadRequest(new { message = "Đơn này không dùng VNPay." });

        if (vnp_ResponseCode == "00" || vnp_TransactionStatus == "00")
        {
            if (order.PaymentStatus != PaymentStatuses.Paid)
            {
                order.PaymentStatus = PaymentStatuses.Paid;
                order.PaidAt = DateTime.UtcNow;
                var txn = string.IsNullOrWhiteSpace(vnp_TxnRef) ? $"VNP-{Guid.NewGuid():N}" : vnp_TxnRef;
                _db.OrderPayments.Add(new OrderPayment
                {
                    OrderId = order.Id,
                    Amount = order.TotalAmount,
                    Kind = PaymentKinds.Online,
                    Method = order.PaymentMethod,
                    Status = PaymentStatuses.Paid,
                    ExternalTransactionId = txn,
                    Note = "Thanh toán VNPay sandbox thành công."
                });
                await _db.SaveChangesAsync();
                _notify.LogEmailStub($"Thanh toán thành công đơn #{order.Id}", $"Mã GD: {txn}");
                await _notify.BroadcastOrderAsync(order);
            }

            return Ok(new
            {
                message = "Thanh toán VNPay thành công.",
                orderId = order.Id,
                paymentStatus = order.PaymentStatus,
                paymentMethod = order.PaymentMethod
            });
        }

        return Ok(new
        {
            message = "Thanh toán VNPay chưa thành công hoặc bị hủy.",
            orderId = order.Id,
            paymentStatus = order.PaymentStatus,
            paymentMethod = order.PaymentMethod
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

        if (order.Status == OrderStatuses.Cancelled)
            return BadRequest(new { message = "Đơn đã bị hủy nên không thể thanh toán nữa." });

        if (order.Status == OrderStatuses.Completed)
            return BadRequest(new { message = "Đơn đã hoàn thành nên không thể thanh toán lại." });

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
    public async Task<IActionResult> MyOrders(int page = 1, int pageSize = 10, string? status = null, string? q = null, string? sortBy = null)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        sortBy = string.IsNullOrWhiteSpace(sortBy) ? "newest" : sortBy.Trim().ToLowerInvariant();

        var query = _db.Orders.AsNoTracking()
            .Include(o => o.Restaurant)
            .Where(o => o.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(o =>
                o.Id.ToString().Contains(term) ||
                (o.Restaurant != null && o.Restaurant.Name.Contains(term)) ||
                (o.Status != null && o.Status.Contains(term)) ||
                (o.PaymentStatus != null && o.PaymentStatus.Contains(term)));
        }

        query = sortBy switch
        {
            "oldest" => query.OrderBy(o => o.OrderDate).ThenBy(o => o.Id),
            "total_asc" => query.OrderBy(o => o.TotalAmount).ThenByDescending(o => o.OrderDate),
            "total_desc" => query.OrderByDescending(o => o.TotalAmount).ThenByDescending(o => o.OrderDate),
            _ => query.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id),
        };

        var total = await query.CountAsync();
        var cancelWindowMinutes = GetSettingInt("Order:CancelWindowMinutes", 10);
        var pageItems = await query
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
                o.CancelReason
            })
            .ToListAsync();

        var items = pageItems.Select(o => new
        {
            o.Id,
            o.RestaurantId,
            o.RestaurantName,
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
            cancelDeadline = o.OrderDate.AddMinutes(cancelWindowMinutes),
            canCancel = o.Status == OrderStatuses.Pending && o.PaymentStatus != PaymentStatuses.Paid && DateTime.UtcNow <= o.OrderDate.AddMinutes(cancelWindowMinutes)
        }).ToList();

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

        var cancelWindowMinutes = GetSettingInt("Order:CancelWindowMinutes", 10);
        var minutesSinceOrder = (DateTime.UtcNow - order.OrderDate).TotalMinutes;
        if (minutesSinceOrder > cancelWindowMinutes)
            return BadRequest(new { message = $"Quá thời gian cho phép hủy đơn ({cancelWindowMinutes} phút)." });

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
        public List<string>? Images { get; set; }
    }

    public class SubmitReviewRequest
    {
        public int OrderId { get; set; }
        public List<ReviewLineRequest> Items { get; set; } = new();
    }

    public class SubmitRestaurantReviewRequest
    {
        public int OrderId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public List<string>? Images { get; set; }
    }

    private static string? NormalizeReviewText(string? text, int maxLength = 2000)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var trimmed = text.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static List<string>? NormalizeReviewImages(List<string>? images, int maxCount = 5)
    {
        if (images == null || images.Count == 0)
            return null;

        return images
            .Where(img => !string.IsNullOrWhiteSpace(img))
            .Select(img => img.Trim())
            .Take(maxCount)
            .ToList();
    }

    /// <summary>Đánh giá món sau khi đơn Completed (một lần cho mỗi món trong đơn).</summary>
    [HttpPost]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest body)
    {
        try
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
            var requestFoodIds = body.Items.Select(x => x.FoodId).ToList();
            if (requestFoodIds.Count != requestFoodIds.Distinct().Count())
                return BadRequest(new { message = "Không được đánh giá trùng món trong cùng một lần gửi." });

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
                var comment = NormalizeReviewText(line.Comment);
                var images = NormalizeReviewImages(line.Images);
                var imagesJson = images != null ? JsonSerializer.Serialize(images) : null;

                _db.FoodReviews.Add(new FoodReview
                {
                    OrderId = order.Id,
                    FoodId = line.FoodId,
                    UserId = userId,
                    Rating = line.Rating,
                    Comment = comment,
                    ImageUrlsJson = imagesJson,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã gửi đánh giá.", orderId = order.Id, count = body.Items.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Không gửi được đánh giá.", detail = ex.ToString() });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SubmitRestaurantReview([FromBody] SubmitRestaurantReviewRequest body)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            if (body.Rating is < 1 or > 5)
                return BadRequest(new { message = "Rating từ 1 đến 5." });

            var order = await _db.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == body.OrderId && o.UserId == userId);
            if (order == null)
                return NotFound(new { message = "Không tìm thấy đơn." });
            if (order.Status != OrderStatuses.Completed)
                return BadRequest(new { message = "Chỉ đánh giá quán sau khi đơn hoàn thành." });
            if (order.RestaurantId <= 0)
                return BadRequest(new { message = "Đơn hàng không có quán hợp lệ." });

            var exists = await _db.RestaurantReviews.AnyAsync(r => r.OrderId == order.Id && r.RestaurantId == order.RestaurantId);
            if (exists)
                return BadRequest(new { message = "Bạn đã đánh giá quán của đơn này rồi." });

            var comment = NormalizeReviewText(body.Comment);
            var images = NormalizeReviewImages(body.Images);
            var imagesJson = images != null ? JsonSerializer.Serialize(images) : null;

            _db.RestaurantReviews.Add(new RestaurantReview
            {
                OrderId = order.Id,
                RestaurantId = order.RestaurantId,
                UserId = userId,
                Rating = body.Rating,
                Comment = comment,
                ImageUrlsJson = imagesJson,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            return Ok(new { message = "Đã gửi đánh giá quán.", orderId = order.Id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Không gửi được đánh giá quán.", detail = ex.ToString() });
        }
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
    public async Task<IActionResult> ChatMessages(int id, int page = 1, int pageSize = 40, string? target = null)
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

        var normalizedTarget = OrderChatHub.NormalizeTarget(target);
        if (order.UserId != userId && !isAdmin && !(isSeller && sellerRestaurantId == order.RestaurantId))
            return StatusCode(403, new { message = "Không xem được chat đơn này." });
        if (isAdmin && normalizedTarget != "admin")
            return StatusCode(403, new { message = "Admin chỉ xem được chat kênh admin." });
        if (isSeller && normalizedTarget != "seller")
            return StatusCode(403, new { message = "Seller chỉ xem được chat kênh seller." });

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = _db.OrderMessages.AsNoTracking()
            .Where(m => m.OrderId == id && OrderChatHub.ParseTargetMeta(m.HiddenReason) == normalizedTarget);
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
                target = OrderChatHub.ParseTargetMeta(m.HiddenReason),
                m.CreatedAt
            })
            .ToListAsync();

        return Ok(new { orderId = id, page, pageSize, total, target = normalizedTarget, items });
    }

    public class CreateReportRequest
    {
        public string TargetType { get; set; } = string.Empty;
        public int TargetId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string? Detail { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportRequest body)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (string.IsNullOrWhiteSpace(body.TargetType) || body.TargetId <= 0 || string.IsNullOrWhiteSpace(body.Reason))
            return BadRequest(new { message = "Thiếu thông tin report." });

        _db.ModerationReports.Add(new ModerationReport
        {
            TargetType = body.TargetType.Trim(),
            TargetId = body.TargetId,
            Reason = body.Reason.Trim(),
            Detail = string.IsNullOrWhiteSpace(body.Detail) ? null : body.Detail.Trim(),
            ReporterUserId = userId,
            Status = "New",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã gửi báo cáo." });
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

        var canCancel = order.UserId == userId && CanCancelOrder(order);
        var cancelDeadline = GetCancelDeadline(order.OrderDate);

        var reviewedFoodIds = await _db.FoodReviews.AsNoTracking()
            .Where(r => r.OrderId == id && !r.IsHidden)
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
            cancelDeadline,
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
