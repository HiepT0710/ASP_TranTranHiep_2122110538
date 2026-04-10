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
        var payload = new
        {
            order.Id,
            order.Status,
            order.TotalAmount,
            order.RestaurantId,
            order.PaymentStatus,
            order.TrackingNumber
        };

        await _hub.Clients.Group(OrderHub.UserGroupName(order.UserId.ToString()))
            .SendAsync("OrderStatusChanged", payload);

        var msg = $"Đơn #{order.Id}: {order.Status}";
        await _push.SendToUserAsync(order.UserId, "Đơn hàng", msg);

        var ownerId = await _db.Restaurants.AsNoTracking()
            .Where(r => r.Id == order.RestaurantId)
            .Select(r => r.OwnerId)
            .FirstOrDefaultAsync();

        if (ownerId != 0 && ownerId != order.UserId)
            await _push.SendToUserAsync(ownerId, "Đơn hàng", msg);
    }

    public void LogEmailStub(string subject, string body) =>
        _log.LogInformation("[Email chưa gửi — demo] {Subject}: {Body}", subject, body);

    public void LogSmsStub(string phone, string body) =>
        _log.LogInformation("[SMS chưa gửi — demo] {Phone}: {Body}", phone, body);
}
