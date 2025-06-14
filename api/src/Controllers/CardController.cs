using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using Boardly.Api.Extensions;
using Boardly.Api.Hubs;
using Boardly.Api.Models.Dtos;
using Boardly.Api.Models.Requests;
using Boardly.Api.Models.Responses;
using Boardly.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using MongoDB.Driver.Linq;

namespace Boardly.Api.Controllers;

[ApiController]
[Route("/boards/{boardId}/cards"), Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public class CardController : ControllerBase
{
    private readonly CardService _cardService;
    private readonly BoardService _boardService;
    private readonly IHubContext<BoardHub, IBoardClient> _boardHubContext;

    public CardController(CardService cardService, BoardService boardService,
        IHubContext<BoardHub, IBoardClient> boardHubContext)
    {
        _cardService = cardService;
        _boardService = boardService;
        _boardHubContext = boardHubContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CardResponse>), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetAllCardsAsync(ObjectId boardId, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        if (await _boardService.GetUserBoardRoleAsync(boardId, userId, null, cancellationToken) == null)
            throw new ForbiddenException("User is not a member of this board.");
        List<CardWithAssignedUserAndTags> cards = await _cardService.GetCardsByBoardId(boardId, null, cancellationToken);
        return Ok(cards.Select(c => new CardResponse(c)));
    }

    [HttpGet("{cardId}")]
    [ProducesResponseType(typeof(CardResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetCardAsync(ObjectId boardId, ObjectId cardId, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        if (await _boardService.GetUserBoardRoleAsync(boardId, userId, null, cancellationToken) == null)
            throw new ForbiddenException("User is not a member of this board.");
        CardWithAssignedUserAndTags card = await _cardService.GetCardById(boardId, cardId, null, cancellationToken)
            ?? throw new RecordDoesNotExist("Card has not been found.");
        return Ok(new CardResponse(card));
    }
    
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> CreateCardAsync(ObjectId boardId, [FromBody] CreateCardRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        var card = new Card
        {
            BoardId = boardId,
            SwimlaneId = data.SwimlaneId,
            ListId = data.ListId,
            Title = data.Title,
            DueDate = data.DueDate,
            Description = data.Description,
            Tags = data.Tags ?? [],
            AssignedUsers = data.AssignedUsers ?? []
        };
        CardWithAssignedUserAndTags newCard = await _cardService.CreateAndFindCardAsync(card, userId, cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).CardCreated(new CardResponse(newCard), cancellationToken);
        return Ok(new IdResponse(card.Id));
    }

    [HttpPatch("{cardId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> UpdateCardAsync(
        ObjectId boardId,
        ObjectId cardId,
        [FromHeader(Name = "If-Match")] DateTime? ifMatch,
        [FromBody] UpdateCardRequest data,
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        var card = new Card
        {
            Id = cardId,
            BoardId = boardId,
            Title = data.Title,
            DueDate = data.DueDate,
            Description = data.Description,
            Tags = data.Tags ?? [],
            AssignedUsers = data.AssignedUsers ?? [],
            UpdatedAt = ifMatch.GetValueOrDefault(),
        };

        CardWithAssignedUserAndTags newCard = await _cardService.UpdateAndFindCardAsync(card, userId, cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).CardUpdated(new CardResponse(newCard), cancellationToken);
        return Ok(new MessageResponse("Card updated successfully!"));
    }
    
    [HttpPatch("{cardId}/move")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> MoveCardAsync(
        ObjectId boardId,
        ObjectId cardId,
        [FromHeader(Name = "If-Match")] DateTime? ifMatch,
        [FromBody] MoveCardRequest data,
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        DateTime updatedAt = await _cardService.MoveCardAsync(cardId, userId, data.ListId, ifMatch.GetValueOrDefault(), cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).CardMoved(cardId, default, data.ListId, updatedAt, cancellationToken);
        return Ok(new MessageResponse("Card moved successfully!"));
    }
    
    [HttpDelete("{cardId}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK,  "application/json")]
    public async Task<IActionResult> DeleteCardAsync
        (ObjectId boardId,
        ObjectId cardId,
        [FromHeader(Name = "If-Match")] DateTime? ifMatch,
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        await _cardService.DeleteCardAsync(cardId, userId, ifMatch.GetValueOrDefault(), cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).CardDeleted(cardId, cancellationToken);
        return Ok(new MessageResponse("Card deleted successfully!"));
    }
}