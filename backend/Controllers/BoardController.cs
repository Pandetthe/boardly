using Boardly.Backend.Entities;
using Boardly.Backend.Exceptions;
using Boardly.Backend.Models.Requests;
using Boardly.Backend.Models.Responses;
using Boardly.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Backend.Controllers;

[ApiController]
[Route("boards"), Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/json")]
public class BoardController(BoardService boardService) : ControllerBase
{
    private readonly BoardService _boardService = boardService;

    [HttpGet]
    [ProducesResponseType(typeof(List<BoardResponse>), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetAllBoardsAsync(CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        List<Board> boards = [.. await _boardService.GetBoardsByUserIdAsync(userId, cancellationToken)];
        return Ok(boards.Select(x => new BoardResponse(x)).ToList());
    }

    [HttpGet("{boardId}")]
    [ProducesResponseType(typeof(DetailedBoardResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetBoardByIdAsync(ObjectId boardId)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Board? board = await _boardService.GetBoardByIdAsync(boardId);
        if (board == null)
            throw new RecordDoesNotExist("Board has not beed found.");
        if (board.Members.All(x => x.UserId != userId))
            throw new ForbidenException("You are not a member of this board.");
        return Ok(new DetailedBoardResponse(board));
    }

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> CreateBoardAsync([FromBody] CreateBoardRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var members = new HashSet<Member>();
        if (data.Members != null)
        {
            foreach (var member in data.Members)
            {
                if (member.UserId == userId)
                {
                    ModelState.AddModelError(nameof(member.UserId), "The board owner cannot be added as a member.");
                    continue;
                }
                members.Add(new Member { UserId = member.UserId, Role = (BoardRole)member.Role });
            }
        }

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var swimlanes = data.Swimlanes?.Select(swimlane => new Swimlane
        {
            Title = swimlane.Title,
            Description = swimlane.Description,
            Tags = swimlane.Tags?.Select(tag => new Tag
            {
                Title = tag.Title,
                Color = tag.Color,
            }).ToList() ?? [],
            Lists = swimlane.Lists?.Select(list => new List
            {
                Title = list.Title,
                Description = list.Description,
                MaxWIP = list.MaxWIP,
            }).ToList() ?? [],
        }).ToHashSet() ?? [];

        var board = new Board
        {
            Title = data.Title,
            Members = [new() { UserId = userId, Role = BoardRole.Owner }, .. members],
            Swimlanes = swimlanes,
        };

        await _boardService.CreateBoardAsync(board, cancellationToken);
        return Ok(new IdResponse(board.Id.ToString()));
    }

    [HttpPatch("{boardId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdateBoardAsync(ObjectId boardId, [FromBody] UpdateBoardRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var members = data.Members.Select(x => new Member { UserId =  x.UserId, Role = (BoardRole)x.Role }).ToHashSet();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var board = new Board
        {
            Id = boardId,
            Title = data.Title,
            Members = members,
        };

        await _boardService.UpdateBoardWithRoleCheckAsync(board, userId, cancellationToken);
        return Ok(new MessageResponse("Successfully updated board!"));
    }

    [HttpDelete("{boardId}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeleteBoardAsync(ObjectId boardId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await _boardService.DeleteBoardWithRoleCheckAsync(boardId, userId, cancellationToken);
        return Ok(new MessageResponse("Board successfully deleted!"));
    }
}
