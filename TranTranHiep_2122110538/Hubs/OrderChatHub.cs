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

    public OrderChatHub(IServiceProvider sp)
    {
        _sp = sp;
    }

    public static string OrderGroup(int orderId) => $"order_{orderId}";

    public async Task JoinOrder(int orderId)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
            return;

        if (!await CanAccessAsync(db, userId, order))
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, OrderGroup(orderId));
    }

    public async Task SendOrderMessage(int orderId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null)
            return;

        if (!await CanAccessAsync(db, userId, order))
            return;

        var trimmed = message.Trim();
        if (trimmed.Length > 4000)
            trimmed = trimmed[..4000];

        var entity = new OrderMessage
        {
            OrderId = orderId,
            UserId = userId,
            Message = trimmed,
            CreatedAt = DateTime.UtcNow
        };
        db.OrderMessages.Add(entity);
        await db.SaveChangesAsync();

        var username = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Username).FirstAsync();

        await Clients.Group(OrderGroup(orderId)).SendAsync("OrderMessageReceived", new
        {
            entity.Id,
            orderId,
            userId,
            username,
            entity.Message,
            entity.CreatedAt
        });

        var push = scope.ServiceProvider.GetRequiredService<IPushNotificationService>();
        var ownerId = await db.Restaurants.AsNoTracking().Where(r => r.Id == order.RestaurantId).Select(r => r.OwnerId).FirstAsync();
        var preview = trimmed.Length <= 80 ? trimmed : trimmed[..80] + "…";
        if (userId == order.UserId)
            await push.SendToUserAsync(ownerId, $"Tin nhắn đơn #{orderId}", preview);
        else if (userId == ownerId || Context.User!.IsInRole(Roles.Admin))
            await push.SendToUserAsync(order.UserId, $"Tin nhắn đơn #{orderId}", preview);
    }

    private static async Task<bool> CanAccessAsync(AppDbContext db, int userId, Order order)
    {
        if (order.UserId == userId)
            return true;

        var role = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Role).FirstOrDefaultAsync();
        if (role == Roles.Admin)
            return true;

        if (role == Roles.Seller)
        {
            var rid = await db.Restaurants.AsNoTracking().Where(r => r.OwnerId == userId).Select(r => r.Id).FirstOrDefaultAsync();
            return rid == order.RestaurantId;
        }

        return false;
    }
}
