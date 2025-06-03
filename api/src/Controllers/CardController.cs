using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using Boardly.Api.Models.Dtos;
using Boardly.Api.Models.Requests;
using Boardly.Api.Models.Responses;
using Boardly.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Api.Controllers;

[ApiController]
[Route("/boards/{boardId}/cards"), Authorize]
public class CardController(CardService cardService, BoardService boardService) : ControllerBase
{
    private readonly CardService _cardService = cardService;
    private readonly BoardService _boardService = boardService;
    
    [HttpGet("{cardId}")]
    [ProducesResponseType(typeof(CardResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetCardAsync(ObjectId cardId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Card card = await _cardService.GetCardByIdAsync(cardId, userId, cancellationToken) ?? throw new RecordDoesNotExist("Card has not been found.");
        BoardWithUser board = await _boardService.GetBoardByIdAsync(card.BoardId, userId, cancellationToken) ?? throw new RecordDoesNotExist("Board has not been found.");
        Swimlane swimlane = board.Swimlanes.FirstOrDefault(x => x.Id == card.SwimlaneId) ?? throw new RecordDoesNotExist("Swimlane has not been found.");
        return Ok(new CardResponse(card, board, swimlane));
    }
    
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> CreateCardAsync(ObjectId boardId, [FromBody] CreateUpdateCardRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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
        return Ok(new IdResponse(card.Id));
    }

    [HttpPatch("{cardId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK,  "application/json")]
    public async Task<IActionResult> UpdateCard(ObjectId cardId, [FromBody] CreateUpdateCardRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var card = await _cardService.GetCardByIdAsync(cardId, userId, cancellationToken) ?? throw new RecordDoesNotExist("Card has not been found.");
        card.Title = data.Title;
        card.DueDate = data.DueDate;
        card.Description = data.Description;
        card.Tags = data.Tags ?? [];
        card.AssignedUsers = data.AssignedUsers ?? [];

        await _cardService.UpdateCardAsync(cardId, userId, card, cancellationToken);
        return Ok(new MessageResponse("Card updated successfully!"));
    }
    
    [HttpPatch("{cardId}/move")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> MoveCard(ObjectId cardId, [FromBody] MoveCardRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await _cardService.MoveCardAsync(cardId, userId, data.SwimlaneId, cancellationToken);
        return Ok(new MessageResponse("Card moved successfully!"));
    }
    
    [HttpDelete("{cardId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK,  "application/json")]
    public async Task<IActionResult> DeleteCard(ObjectId cardId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await _cardService.DeleteCardAsync(cardId, userId, cancellationToken);
        return Ok(new MessageResponse("Card deleted successfully!"));
    }
    
}