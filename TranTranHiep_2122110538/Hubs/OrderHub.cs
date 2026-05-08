using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TranTranHiep_2122110538.Data;
using TranTranHiep_2122110538.Infrastructure;
using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Hubs;

public class OrderHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.User?.Identity?.IsAuthenticated == true)
        {
            await JoinAllGroupsAsync();
        }

        await base.OnConnectedAsync();
    }

    private async Task JoinAllGroupsAsync()
    {
        var user = Context.User;
        var id = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(id)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(id));

        if (user!.IsInRole(Roles.Admin))
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroupName());

        if (user.IsInRole(Roles.Seller))
        {
            using var scope = Context.GetHttpContext()!.RequestServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userId = int.Parse(id);
            var restaurantIds = await db.Restaurants.AsNoTracking()
                .Where(r => r.OwnerId == userId)
                .Select(r => r.Id)
                .ToListAsync();

            foreach (var rid in restaurantIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, SellerGroupName(rid));
        }
    }

    [Authorize]
    public Task JoinUserGroup() => Task.CompletedTask;

    [Authorize]
    public Task JoinAdminGroup() => Task.CompletedTask;

    [Authorize]
    public Task JoinSellerGroup() => Task.CompletedTask;

    public static string UserGroupName(string userId) => $"user_{userId}";
    public static string AdminGroupName() => "admin_notifications";
    public static string SellerGroupName(int restaurantId) => $"seller_{restaurantId}";
}
