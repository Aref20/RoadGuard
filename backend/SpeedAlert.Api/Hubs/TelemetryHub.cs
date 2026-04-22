using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SpeedAlert.Api.Hubs;

[Authorize(Roles = "Admin")]
public class TelemetryHub : Hub
{
    public const string AdminGroup = "admins";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(System.Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, AdminGroup);
        await base.OnDisconnectedAsync(exception);
    }
}
