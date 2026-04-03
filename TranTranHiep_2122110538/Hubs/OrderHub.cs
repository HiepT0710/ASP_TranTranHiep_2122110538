using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TranTranHiep_2122110538.Hubs;

public class OrderHub : Hub
{
    /// <summary>Client gọi sau khi đăng nhập để nhận cập nhật trạng thái đơn.</summary>
    [Authorize]
    public Task JoinUserGroup()
    {
        var id = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(id))
            return Task.CompletedTask;
        return Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(id));
    }

    public static string UserGroupName(string userId) => $"user_{userId}";
}
