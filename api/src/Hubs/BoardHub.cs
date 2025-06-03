using Boardly.Api.Entities.Board;
using Boardly.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Api.Hubs;

[Authorize]
public class BoardHub : Hub
{
    private readonly BoardService _boardService;

    public BoardHub(BoardService boardService)
    {
        _boardService = boardService;
    }

    public override async Task OnConnectedAsync()
    {
        ObjectId userId = ObjectId.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        string? boardIdString = Context.GetHttpContext()?.Request.Query["boardId"];
        if (!ObjectId.TryParse(boardIdString, out ObjectId boardId))
        {
            Context.Abort();
            return;
        }
        BoardRole? role = await _boardService.CheckUserBoardRoleAsync(boardId, userId);
        if (role == null)
        {
            Context.Abort();
            return;
        }

        await _boardService.ChangeUserActivity(boardId, userId, true);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        ObjectId userId = ObjectId.Parse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        string? boardIdString = Context.GetHttpContext()?.Request.Query["boardId"];
        if (ObjectId.TryParse(boardIdString, out ObjectId boardId))
            await _boardService.ChangeUserActivity(boardId, userId, false);

        await base.OnDisconnectedAsync(exception);
    }
}
