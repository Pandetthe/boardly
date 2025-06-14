using Boardly.Api.Entities.Board;
using Boardly.Api.Extensions;
using Boardly.Api.Models.Dtos;
using Boardly.Api.Models.Requests;
using Boardly.Api.Models.Responses;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;

namespace Boardly.Api.Hubs;

public partial class BoardHub
{
    public async Task CreateCard(CreateCardRequest data)
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
        Card card = new()
        {
            Title = data.Title,
            Description = data.Description,
            BoardId = boardId,
            ListId = data.ListId,
            SwimlaneId = data.SwimlaneId,
            AssignedUsers = data.AssignedUsers ?? [],
            Tags = data.Tags ?? [],
            DueDate = data.DueDate,
        };
        CardWithAssignedUserAndTags newCard = await _cardService.CreateAndFindCardAsync(card, userId.Value, Context.ConnectionAborted);
        await Clients.Group(boardId.ToString()).CardCreated(
            new CardResponse(newCard), Context.ConnectionAborted);
    }

    public async Task UpdateCard(ObjectId cardId, UpdateCardRequest data, DateTime? updatedAt)
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
        Card card = new()
        {
            Id = cardId,
            Title = data.Title,
            Description = data.Description,
            BoardId = boardId,
            AssignedUsers = data.AssignedUsers ?? [],
            Tags = data.Tags ?? [],
            DueDate = data.DueDate,
            UpdatedAt = updatedAt.GetValueOrDefault()
        };
        CardWithAssignedUserAndTags newCard = await _cardService.UpdateAndFindCardAsync(card, userId.Value, Context.ConnectionAborted);
        await Clients.Group(boardId.ToString()).CardUpdated(
                new CardResponse(newCard), Context.ConnectionAborted);
    }

    public async Task MoveCard(ObjectId swimlaneId, ObjectId cardId, ObjectId newListId, DateTime? updatedAt)
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
        DateTime newUpdatedAt = await _cardService.MoveCardAsync(
            cardId, userId.Value, newListId, updatedAt.GetValueOrDefault(), Context.ConnectionAborted);
        await Clients.Group(boardId.ToString()).CardMoved(cardId, swimlaneId, newListId, newUpdatedAt, Context.ConnectionAborted);
    }

    public async Task DeleteCard(ObjectId cardId, DateTime? updatedAt)
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
        await _cardService.DeleteCardAsync(cardId, userId.Value, updatedAt.GetValueOrDefault(), Context.ConnectionAborted);
        await Clients.Group(boardId.ToString()).CardDeleted(cardId, Context.ConnectionAborted);
    }
    
    public async Task LockCard(ObjectId cardId)
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
        if (_cardService.LockCard(cardId, userId.Value))
        {
            SimplifiedUser user = await _cardService.GetLockingUserById(userId.Value, null, Context.ConnectionAborted)
                ?? throw new InvalidOperationException("Locking user not found for card");
            await Clients.Group(boardId.ToString()).CardLocked(cardId, new SimplifiedUserResponse(user), Context.ConnectionAborted);
        }
    }

    public async Task UnlockCard(ObjectId cardId)
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
        _cardService.UnlockCard(cardId, userId.Value);
        await Clients.Group(boardId.ToString()).CardUnlocked(cardId, Context.ConnectionAborted);
    }
}
