using Boardly.Backend.Entities;
using Boardly.Backend.Models;
using Boardly.Backend.Models.Board;
using Boardly.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class BoardController(BoardService boardService) : ControllerBase
{
    private readonly BoardService _boardService = boardService;

    [HttpGet]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(List<BoardResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUserBoardsAsync(CancellationToken cancellationToken)
    {
        ObjectId userId = new(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        List<Board> boards = (await _boardService.GetBoardsByUserIdAsync(userId, cancellationToken)).ToList();
        return Ok(boards.Select(x => new BoardResponse
        {
            Id = x.Id.ToString(),
            Title = x.Title,
            Description = x.Description,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
        }).ToList());
    }

    [HttpGet("{boardId}")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BoardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBoardByIdAsync(string boardId)
    {
        if (!ObjectId.TryParse(boardId, out ObjectId boardObjectId))
            return BadRequest(new MessageResponse("Invalid board ID format."));
        ObjectId userId = new(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Board? board = await _boardService.GetBoardByIdAsync(boardObjectId);
        if (board == null)
            return NotFound(new MessageResponse("Board not found."));
        if (board.Members.All(x => x.UserId != userId))
            return Unauthorized(new MessageResponse("You are not a member of this board."));
        return Ok(new BoardResponse
        {
            Id = board.Id.ToString(),
            Title = board.Title,
            Description = board.Description,
            CreatedAt = board.CreatedAt,
            UpdatedAt = board.UpdatedAt,
        });
    }

    [HttpPost]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddAsync([FromBody]CreateRequest data, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);
        ObjectId userId = new(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var board = new Board
        {
            Title = data.Title,
            Description = data.Description,
            Members = [new() { UserId = userId }]
        };
        await _boardService.AddBoardAsync(board, cancellationToken);
        return Ok(new IdResponse(board.Id.ToString()));
    }

    [HttpPatch("{boardId}")]
    [Authorize]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateBoardAsync(string boardId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(boardId, out ObjectId boardObjectId))
            return BadRequest(new MessageResponse("Invalid board ID format."));
        ObjectId userId = new(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return Ok();
    }

    [HttpDelete("{boardId}")]
    [Authorize]
    public async Task<IActionResult> DeleteBoardAsync(string boardId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(boardId, out ObjectId boardObjectId))
            return BadRequest(new MessageResponse("Invalid board ID format."));
        ObjectId userId = new(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return Ok();
    }
}
