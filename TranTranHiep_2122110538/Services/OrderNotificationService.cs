using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Hubs;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Services;

public class OrderNotificationService : IOrderNotificationService
{
    private readonly IHubContext<OrderHub> _hub;
    private readonly ILogger<OrderNotificationService> _log;
    private readonly IPushNotificationService _push;
    private readonly AppDbContext _db;

    public OrderNotificationService(
        IHubContext<OrderHub> hub,
        ILogger<OrderNotificationService> log,
        IPushNotificationService push,
        AppDbContext db)
    {
        _hub = hub;
        _log = log;
        _push = push;
        _db = db;
    }

    public async Task BroadcastOrderAsync(Order order)
    {
        var ownerId = await _db.Restaurants.AsNoTracking()
            .Where(r => r.Id == order.RestaurantId)
            .Select(r => r.OwnerId)
            .FirstOrDefaultAsync();

        var payload = new
        {
            order.Id,
            order.Status,
            order.TotalAmount,
            order.RestaurantId,
            order.PaymentStatus,
            order.TrackingNumber,
            order.CancelReason,
            order.CancelledAt,
            order.CancelledBy
        };

        var baseMessage = order.Status switch
        {
            OrderStatuses.Cancelled => $"Đơn #{order.Id} đã bị hủy",
            OrderStatuses.Completed => $"Đơn #{order.Id} đã hoàn thành",
            _ => $"Đơn #{order.Id}: {order.Status}"
        };

        await _hub.Clients.Group(OrderHub.UserGroupName(order.UserId.ToString()))
            .SendAsync("OrderStatusChanged", payload);

        if (ownerId != 0 && ownerId != order.UserId)
        {
            await _hub.Clients.Group(OrderHub.SellerGroupName(order.RestaurantId))
                .SendAsync(order.Status == OrderStatuses.Cancelled ? "OrderCancelled" : order.Status == OrderStatuses.Pending ? "OrderCreated" : "OrderStatusChanged", payload);

            await _push.SendToUserAsync(ownerId, "Đơn hàng", baseMessage);
        }

        if (order.Status != OrderStatuses.Pending)
        {
            await _hub.Clients.Group(OrderHub.AdminGroupName())
                .SendAsync(order.Status == OrderStatuses.Cancelled ? "OrderCancelled" : "OrderStatusChanged", payload);
        }

        await _push.SendToUserAsync(order.UserId, "Đơn hàng", baseMessage);

        if (ownerId != 0 && ownerId != order.UserId)
            await _push.SendToUserAsync(ownerId, "Đơn hàng", baseMessage);
    }

    public void LogEmailStub(string subject, string body) =>
        _log.LogInformation("[Email chưa gửi — demo] {Subject}: {Body}", subject, body);

    public void LogSmsStub(string phone, string body) =>
        _log.LogInformation("[SMS chưa gửi — demo] {Phone}: {Body}", phone, body);
}
