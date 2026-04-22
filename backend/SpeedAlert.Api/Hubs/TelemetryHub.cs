using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace SpeedAlert.Api.Hubs;

public class TelemetryHub : Hub
{
    // Clients will connect and just listen to events broadcast by the server.
    // E.g., ReceiveHealth, ReceiveUsers, ReceiveSessions
}
