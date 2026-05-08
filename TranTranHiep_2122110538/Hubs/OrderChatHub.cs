using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Models;
using TranTranHiep_2122110538.Services;

namespace TranTranHiep_2122110538.Hubs;

/// <summary>Chat theo đơn — client gọi JoinOrder(orderId) rồi SendOrderMessage(orderId, text).</summary>
[Authorize]
public class OrderChatHub : Hub
{
    private readonly IServiceProvider _sp;
    private const string TargetMetaPrefix = "chat-target:";

    public OrderChatHub(IServiceProvider sp)
    {
        _sp = sp;
    }

    public static string OrderGroup(int orderId) => $"order_{orderId}";
    public static string UserInboxGroup(int userId) => $"chat_user_{userId}";
    public static string SellerInboxGroup(int userId) => $"chat_seller_{userId}";
    public static string AdminInboxGroup() => "chat_admin";

    public static string NormalizeTarget(string? target)
    {
        var normalized = (target ?? "seller").Trim().ToLowerInvariant();
        return normalized == "admin" ? "admin" : "seller";
    }

    public static string BuildTargetMeta(string target) => $"{TargetMetaPrefix}{NormalizeTarget(target)}";

    public static string ParseTargetMeta(string? hiddenReason)
    {
        if (string.IsNullOrWhiteSpace(hiddenReason))
            return "seller";

        if (!hiddenReason.StartsWith(TargetMetaPrefix, StringComparison.OrdinalIgnoreCase))
            return "seller";

        var raw = hiddenReason[TargetMetaPrefix.Length..].Trim();
        return NormalizeTarget(raw);
    }

    public override async Task OnConnectedAsync()
    {
        await JoinAvailableGroupsAsync();
        await base.OnConnectedAsync();
    }

    private async Task JoinAvailableGroupsAsync()
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Role).FirstOrDefaultAsync();

        if (role == Roles.User)
        {
            var orderIds = await db.Orders.AsNoTracking()
                .Where(o => o.UserId == userId)
                .Select(o => o.Id)
                .ToListAsync();
            foreach (var orderId in orderIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroup(orderId));
            await Groups.AddToGroupAsync(Context.ConnectionId, UserInboxGroup(userId));
        }
        else if (role == Roles.Seller)
        {
            var restaurantIds = await db.Restaurants.AsNoTracking()
                .Where(r => r.OwnerId == userId)
                .Select(r => r.Id)
                .ToListAsync();
            var orderIds = await db.Orders.AsNoTracking()
                .Where(o => restaurantIds.Contains(o.RestaurantId))
                .Select(o => o.Id)
                .ToListAsync();
            foreach (var orderId in orderIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroup(orderId));
            await Groups.AddToGroupAsync(Context.ConnectionId, SellerInboxGroup(userId));
        }
        else if (role == Roles.Admin)
        {
            // Admin only joins the general inbox by default.
            // Access to a specific order chat should be granted explicitly when the user chooses admin support.
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminInboxGroup());
        }
    }

    public async Task JoinOrder(int orderId)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
            return;

        if (!await CanAccessAsync(db, userId, order, "seller"))
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroup(orderId));
    }

    public async Task SendOrderMessage(int orderId, string message, string target = "seller")
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
            return;

        var normalizedTarget = NormalizeTarget(target);
        if (!await CanAccessAsync(db, userId, order, normalizedTarget))
            return;

        var trimmed = message.Trim();
        if (trimmed.Length > 4000)
            trimmed = trimmed[..4000];

        var entity = new OrderMessage
        {
            OrderId = orderId,
            UserId = userId,
            Message = trimmed,
            HiddenReason = BuildTargetMeta(normalizedTarget),
            CreatedAt = DateTime.UtcNow
        };
        db.OrderMessages.Add(entity);
        await db.SaveChangesAsync();

        var username = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Username).FirstAsync();

        var payload = new
        {
            entity.Id,
            orderId,
            userId,
            username,
            message = entity.Message,
            target = normalizedTarget,
            entity.CreatedAt
        };

        var ownerId = await db.Restaurants.AsNoTracking().Where(r => r.Id == order.RestaurantId).Select(r => r.OwnerId).FirstAsync();
        if (normalizedTarget == "admin")
        {
            await Clients.Groups(UserInboxGroup(order.UserId), AdminInboxGroup()).SendAsync("OrderMessageReceived", payload);
        }
        else
        {
            await Clients.Groups(UserInboxGroup(order.UserId), SellerInboxGroup(ownerId)).SendAsync("OrderMessageReceived", payload);
        }

        var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        var preview = trimmed.Length <= 80 ? trimmed : trimmed[..80] + "…";
        if (normalizedTarget == "admin")
        {
            if (userId == order.UserId)
            {
                var adminIds = await db.Users.AsNoTracking().Where(u => u.Role == Roles.Admin).Select(u => u.Id).ToListAsync();
                foreach (var adminId in adminIds)
                    await push.SendToUserAsync(adminId, $"Tin nhắn hỗ trợ admin đơn #{orderId}", preview);
            }
            else if (Context.User!.IsInRole(Roles.Admin))
            {
                await push.SendToUserAsync(order.UserId, $"Phản hồi admin đơn #{orderId}", preview);
            }
        }
        else if (userId == order.UserId)
        {
            await push.SendToUserAsync(ownerId, $"Tin nhắn đơn #{orderId}", preview);
        }
        else if (userId == ownerId)
        {
            await push.SendToUserAsync(order.UserId, $"Tin nhắn đơn #{orderId}", preview);
        }
    }

    private static async Task<bool> CanAccessAsync(AppDbContext db, int userId, Order order, string target)
    {
        if (order.UserId == userId)
            return true;

        var role = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Role).FirstOrDefaultAsync();
        if (role == Roles.Admin)
            return target == "admin";

        if (role == Roles.Seller)
        {
            if (target != "seller")
                return false;
            var rid = await db.Restaurants.AsNoTracking().Where(r => r.OwnerId == userId).Select(r => r.Id).FirstOrDefaultAsync();
            return rid == order.RestaurantId;
        }

        return false;
    }
}
