using Boardly.Api.Entities.Board;
using Boardly.Api.Extensions;
using Boardly.Api.Models.Dtos;
using Boardly.Api.Models.Requests;
using Boardly.Api.Models.Responses;
using Boardly.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;

namespace Boardly.Api.Hubs;

[Authorize]
public partial class BoardHub : Hub<IBoardClient>
{
    private readonly BoardService _boardService;
    private readonly SwimlaneService _swimlaneService;
    private readonly CardService _cardService;
    private readonly ILogger<BoardHub> _logger;

    public BoardHub(BoardService boardService,
        SwimlaneService swimlaneService, CardService cardService,
        ILogger<BoardHub> logger)
    {
        _boardService = boardService;
        _swimlaneService = swimlaneService;
        _cardService = cardService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        ObjectId? userId = Context.User?.GetUserId();
        var connectionId = Context.ConnectionId;

        if (userId == null)
        {
            _logger.LogWarning("Connection {ConnectionId} aborted: userId not found", connectionId);
            Context.Abort();
            return;
        }

        string? boardIdString = Context.GetHttpContext()?.Request.Query["boardId"];
        if (!ObjectId.TryParse(boardIdString, out ObjectId boardId))
        {
            _logger.LogWarning("Connection {ConnectionId} aborted: invalid boardId query parameter '{BoardIdString}'", connectionId, boardIdString);
            Context.Abort();
            return;
        }

        BoardRole? role = await _boardService.GetUserBoardRoleAsync(boardId, userId.Value, null, Context.ConnectionAborted);
        if (role == null)
        {
            _logger.LogWarning("Connection {ConnectionId} aborted: user {UserId} has no role on board {BoardId}", connectionId, userId, boardId);
            Context.Abort();
            return;
        }

        _logger.LogInformation("User {UserId} connected with connection {ConnectionId} to board {BoardId} with role {Role}", userId, connectionId, boardId, role);

        await _boardService.ChangeUserActivity(boardId, userId.Value, true, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(connectionId, boardId.ToString(), Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }

    public async Task UpdateBoard(UpdateBoardRequest data, DateTime updatedAt)
    {
        ObjectId? userId = Context.User?.GetUserId();
        if (userId == null)
        {
            Context.Abort();
            return;
        }
        string? boardIdString = Context.GetHttpContext()?.Request.Query["boardId"];
        if (!ObjectId.TryParse(boardIdString, out ObjectId boardId))
        {
            Context.Abort();
            return;
        }
        Board board = new()
        {
            Id = boardId,
            Title = data.Title,
            Members = data.Members?.Select(m => new Member
            {
                UserId = m.UserId,
                Role = m.Role
            }).ToHashSet() ?? [],
            UpdatedAt = updatedAt
        };
        BoardWithUser newBoard = await _boardService.UpdateAndFindBoardAsync(board, userId.Value, Context.ConnectionAborted);
        await Clients.Group(boardId.ToString()).BoardUpdated(new BoardResponse(newBoard), Context.ConnectionAborted);
    }

    public async Task CreateSwimlane(CreateSwimlaneRequest data, DateTime updatedAt)
    {
        ObjectId? userId = Context.User?.GetUserId();
        if (userId == null)
        {
            Context.Abort();
            return;
        }
        string? boardIdString = Context.GetHttpContext()?.Request.Query["boardId"];
        if (!ObjectId.TryParse(boardIdString, out ObjectId boardId))
        {
            Context.Abort();
            return;
        }
        var swimlane = new Swimlane
        {
            Title = data.Title,
            Tags = [.. data.Tags?.Select(tag => new Tag
        {
            Title = tag.Title,
            Color = tag.Color,
        }) ?? []],
            Lists = [..data.Lists?.Select(list => new List
        {
            Title = list.Title,
            MaxWIP = list.MaxWIP,
        }) ?? []],
        };

        (Swimlane newSwimlane, DateTime newUpdatedAt) = await _swimlaneService
            .CreateAndFindSwimlaneAsync(boardId, userId.Value, swimlane, updatedAt, Context.ConnectionAborted);
        await Clients.Group(boardId.ToString()).SwimlaneCreated(
            new DetailedSwimlaneResponse(newSwimlane), updatedAt, Context.ConnectionAborted);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        ObjectId? userId = Context.User?.GetUserId();
        var connectionId = Context.ConnectionId;

        string? boardIdString = Context.GetHttpContext()?.Request.Query["boardId"];

        if (userId == null)
        {
            _logger.LogWarning("Disconnected connection {ConnectionId} with no userId", connectionId);
            Context.Abort();
            return;
        }

        if (ObjectId.TryParse(boardIdString, out ObjectId boardId))
        {
            _logger.LogInformation("User {UserId} disconnected with connection {ConnectionId} from board {BoardId}", userId, connectionId, boardId);
            await _boardService.ChangeUserActivity(boardId, userId.Value, false);
            _cardService.UnlockAllLockedByUser(userId.Value);
            await Groups.RemoveFromGroupAsync(connectionId, boardId.ToString());
        }
        else
        {
            _logger.LogWarning("Disconnected connection {ConnectionId} user {UserId} had invalid or missing boardId query parameter '{BoardIdString}'", connectionId, userId, boardIdString);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
