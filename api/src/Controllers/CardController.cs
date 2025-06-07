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
    private readonly IHubContext<BoardHub> _boardHubContext;

    public CardController(CardService cardService, IHubContext<BoardHub> boardHubContext)
    {
        _boardHubContext = boardHubContext;
        _cardService = cardService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CardResponse>), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetAllCardsAsync(ObjectId boardId, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        IEnumerable<CardWithAssignedUserAndTags> cards =
            await _cardService.GetCardsByBoardIdAsync(boardId, userId, cancellationToken);
        return Ok(cards.Select(x => new CardResponse(x)));
    }

    [HttpGet("{cardId}")]
    [ProducesResponseType(typeof(CardResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetCardAsync(ObjectId boardId, ObjectId cardId, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        CardWithAssignedUserAndTags card = await _cardService.GetCardByIdAsync(boardId, cardId, userId, cancellationToken)
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
        await _cardService.CreateCardAsync(userId, card, cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SendAsync("CardCreate", cancellationToken);
        return Ok(new IdResponse(card.Id));
    }

    [HttpPatch("{cardId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> UpdateCardAsync(ObjectId boardId, ObjectId cardId, [FromBody] UpdateCardRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        var card = await _cardService.GetRawCardByIdAsync(boardId, cardId, userId, cancellationToken) ?? throw new RecordDoesNotExist("Card has not been found.");
        card.Title = data.Title;
        card.DueDate = data.DueDate;
        card.Description = data.Description;
        card.Tags = data.Tags ?? [];
        card.AssignedUsers = data.AssignedUsers ?? [];

        await _cardService.UpdateCardAsync(cardId, userId, card, cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SendAsync("CardUpdate", cancellationToken);
        return Ok(new MessageResponse("Card updated successfully!"));
    }
    
    [HttpPatch("{cardId}/move")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> MoveCardAsync(ObjectId boardId, ObjectId cardId, [FromBody] MoveCardRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        await _cardService.MoveCardAsync(cardId, userId, data.ListId, cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SendAsync("CardMove", cancellationToken);
        return Ok(new MessageResponse("Card moved successfully!"));
    }
    
    [HttpDelete("{cardId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK,  "application/json")]
    public async Task<IActionResult> DeleteCardAsync(ObjectId boardId, ObjectId cardId, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        await _cardService.DeleteCardAsync(cardId, userId, cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SendAsync("CardDelete", cancellationToken);
        return Ok(new MessageResponse("Card deleted successfully!"));
    }
}