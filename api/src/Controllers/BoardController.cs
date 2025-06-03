using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using Boardly.Api.Hubs;
using Boardly.Api.Models.Dtos;
using Boardly.Api.Models.Requests;
using Boardly.Api.Models.Responses;
using Boardly.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Api.Controllers;

[ApiController]
[Route("boards"), Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public class BoardController : ControllerBase
{
    private readonly BoardService _boardService;
    private readonly IHubContext<BoardHub> _hubContext;

    public BoardController(
        BoardService boardService,
        IHubContext<BoardHub> hubContext)
    {
        _boardService = boardService;
        _hubContext = hubContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<BoardResponse>), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetAllBoardsAsync(CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        List<BoardWithUser> boards = [.. await _boardService.GetBoardsByUserIdAsync(userId, cancellationToken)];
        return Ok(boards.Select(x => new BoardResponse(x)).ToList());
    }

    [HttpGet("{boardId}")]
    [ProducesResponseType(typeof(DetailedBoardResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetBoardByIdAsync(ObjectId boardId)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        BoardWithUser? board = await _boardService.GetBoardByIdAsync(boardId, userId)
            ?? throw new RecordDoesNotExist("Board has not beed found.");
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
            Color = swimlane.Color,
            Tags = swimlane.Tags?.Select(tag => new Tag
            {
                Title = tag.Title,
                Color = tag.Color,
            }).ToList() ?? [],
            Lists = swimlane.Lists?.Select(list => new List
            {
                Title = list.Title,
                MaxWIP = list.MaxWIP,
                Color = list.Color,
            }).ToList() ?? [],
        }).ToHashSet() ?? [];

        var board = new Board
        {
            Title = data.Title,
            Members = [new() { UserId = userId, Role = BoardRole.Owner }, .. members],
            Swimlanes = swimlanes,
        };

        await _boardService.CreateBoardAsync(board, cancellationToken);
        return Ok(new IdResponse(board.Id));
    }

    [HttpPatch("{boardId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdateBoardAsync(ObjectId boardId, [FromBody] UpdateBoardRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var members = data.Members.Select(x => new Member { UserId =  x.UserId, Role = x.Role }).ToHashSet();

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var board = new Board
        {
            Id = boardId,
            Title = data.Title,
            Members = members,
        };

        await _boardService.UpdateBoardAsync(board, userId, cancellationToken);
        return Ok(new MessageResponse("Successfully updated board!"));
    }

    [HttpDelete("{boardId}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DeleteBoardAsync(ObjectId boardId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await _boardService.DeleteBoardAsync(boardId, userId, cancellationToken);
        return Ok(new MessageResponse("Board successfully deleted!"));
    }
}
