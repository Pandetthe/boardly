using Boardly.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Api.Hubs;

[Authorize]
public class BoardHub : Hub
{
    public async Task UpdateBoard(ObjectId boardId, object boardData)
    {
        ObjectId userId = ObjectId.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await Clients.GroupExcept(boardId.ToString(), Context.ConnectionId)
            .SendAsync("BoardUpdated", boardId, boardData);
    }

    public async Task DeleteBoard(ObjectId boardId)
    {
        ObjectId userId = ObjectId.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await Clients.GroupExcept(boardId.ToString(), Context.ConnectionId)
            .SendAsync("BoardDeleted", boardId);
    }

    public async Task JoinBoard(ObjectId boardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, boardId.ToString());
    }

    public async Task LeaveBoard(ObjectId boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, boardId.ToString());
    }
}
