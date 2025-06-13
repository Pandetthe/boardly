using Boardly.Api.Entities;
using Boardly.Api.Entities.Board;
using Boardly.Api.Extensions;
using Boardly.Api.Models.Dtos;
using Boardly.Api.Models.Requests;
using Boardly.Api.Models.Responses;
using Boardly.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using System.Text.RegularExpressions;
using System.Threading;

namespace Boardly.Api.Hubs;

[Authorize]
public class BoardHub : Hub<IBoardClient>
{
    private readonly BoardService _boardService;
    private readonly SwimlaneService _swimlaneService;
    private readonly CardService _cardService;

    public BoardHub(BoardService boardService,
        SwimlaneService swimlaneService, CardService cardService)
    {
        _boardService = boardService;
        _swimlaneService = swimlaneService;
        _cardService = cardService;
    }

    public override async Task OnConnectedAsync()
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
        BoardRole? role = await _boardService.GetUserBoardRoleAsync(boardId, userId.Value, Context.ConnectionAborted);
        if (role == null)
        {
            Context.Abort();
            return;
        }

        await _boardService.ChangeUserActivity(boardId, userId.Value, true, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, boardId.ToString(), Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }

    public async Task StartCardEditing(ObjectId cardId)
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
        if (_cardService.StartCardEditing(cardId, userId.Value))
            await Clients.Group(boardId.ToString()).CardEditingStarted(cardId, userId.Value);
    }

    public async Task FinishCardEditing(ObjectId cardId, UpdateCardRequest data, DateTime updatedAt)
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
        try
        {
            Card card = new()
            {
                Id = cardId,
                BoardId = boardId,
                Title = data.Title,
                Description = data.Description,
                AssignedUsers = data.AssignedUsers ?? [],
                Tags = data.Tags ?? [],
                DueDate = data.DueDate,
                UpdatedAt = updatedAt
            };
            await _cardService.FinishCardEditing(card, userId.Value, Context.ConnectionAborted);
            CardWithAssignedUserAndTags? newCard = await _cardService.GetCardByIdAsync(cardId, boardId, userId.Value, Context.ConnectionAborted);
            if (newCard != null)
                await Clients.Group(boardId.ToString()).CardEditingFinished(new CardResponse(newCard));
        }
        catch(Exception ex)
        {
            await Clients.Caller.Error(ex.Message);
        }
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
        try
        {
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
        catch (Exception ex)
        {
            await Clients.Caller.Error(ex.Message);
        }
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
        try
        {
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
        catch (Exception ex)
        {
            await Clients.Caller.Error(ex.Message);
        }
    }

    public async Task MoveCard(ObjectId swimlaneId, ObjectId cardId, ObjectId newListId, DateTime updatedAt = default)
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
        try
        {
            DateTime newUpdatedAt = await _cardService.MoveCardAsync(
                cardId, userId.Value, newListId, updatedAt, Context.ConnectionAborted);
            await Clients.Group(boardId.ToString()).CardMoved(swimlaneId, cardId, newListId, newUpdatedAt, Context.ConnectionAborted);
        }
        catch (Exception ex)
        {
            await Clients.Caller.Error(ex.Message);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        ObjectId? userId = Context.User?.GetUserId();
        if (userId == null)
        {
            Context.Abort();
            return;
        }
        string? boardIdString = Context.GetHttpContext()?.Request.Query["boardId"];
        if (ObjectId.TryParse(boardIdString, out ObjectId boardId))
        {
            await _boardService.ChangeUserActivity(boardId, userId.Value, false);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, boardId.ToString());
        }

        await base.OnDisconnectedAsync(exception);
    }
}
