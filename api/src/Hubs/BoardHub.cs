using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Api.Hubs;

[Authorize]
public class BoardHub : Hub
{
    public override Task OnConnectedAsync()
    {
        ObjectId userId = ObjectId.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        ObjectId userId = ObjectId.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        return base.OnDisconnectedAsync(exception);
    }
}
