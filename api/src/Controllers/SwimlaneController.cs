using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using Boardly.Api.Extensions;
using Boardly.Api.Hubs;
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
[Route("boards/{boardId}/swimlanes"), Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public class SwimlaneController : ControllerBase
{
    private readonly SwimlaneService _swimlaneService;
    private readonly BoardService _boardService;
    private readonly IHubContext<BoardHub, IBoardClient> _boardHubContext;

    public SwimlaneController(SwimlaneService swimlaneService, BoardService boardService,
        IHubContext<BoardHub, IBoardClient> boardHubContext)
    {
        _swimlaneService = swimlaneService;
        _boardService = boardService;
        _boardHubContext = boardHubContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SwimlaneResponse>), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetAllSwimlanesAsync(ObjectId boardId, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        if (await _boardService.GetUserBoardRoleAsync(boardId, userId, cancellationToken) == null)
            throw new ForbiddenException("User is not a member of this board.");
        List<Swimlane> swimlanes = await _swimlaneService.GetSwimlanesByBoardId(boardId).ToListAsync(cancellationToken);
        return Ok(swimlanes.Select(x => new SwimlaneResponse(x)).ToList());
    }

    [HttpGet("{swimlaneId}")]
    [ProducesResponseType(typeof(DetailedSwimlaneResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetSwimlaneByIdAsync(ObjectId boardId, ObjectId swimlaneId, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        if (await _boardService.GetUserBoardRoleAsync(boardId, userId, cancellationToken) == null)
            throw new ForbiddenException("User is not a member of this board.");
        Swimlane swimlane = await _swimlaneService.GetSwimlanesByBoardId(boardId).FirstOrDefaultAsync(s => s.Id == swimlaneId, cancellationToken)
            ?? throw new RecordDoesNotExist("Swimlane has not been found.");
        return Ok(new DetailedSwimlaneResponse(swimlane));
    }


    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> CreateSwimlaneAsync(
        ObjectId boardId,
        [FromBody] CreateSwimlaneRequest data,
        [FromHeader(Name = "If-Match")] DateTime? ifMatch,
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
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
                Color = list.Color,
                MaxWIP = list.MaxWIP,
            }) ?? []],
        };

        (Swimlane newSwimlane, DateTime updatedAt) = await _swimlaneService.CreateAndFindSwimlaneAsync(boardId, userId,
            swimlane, ifMatch.GetValueOrDefault(), cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SwimlaneCreated(
            new DetailedSwimlaneResponse(newSwimlane), updatedAt, cancellationToken);
        return Ok(new IdResponse(swimlane.Id));
    }

    [HttpPatch("{swimlaneId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> UpdateSwimlaneAsync(
        ObjectId boardId,
        ObjectId swimlaneId,
        [FromBody] UpdateSwimlaneRequest data,
        [FromHeader(Name = "If-Match")] DateTime? ifMatch,
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        var swimlane = new Swimlane
        {
            Id = swimlaneId,
            Title = data.Title,
        };

        (Swimlane newSwimlane, DateTime updatedAt) = await _swimlaneService.UpdateAndFindSwimlaneAsync(boardId, userId,
            swimlane, ifMatch.GetValueOrDefault(), cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SwimlaneUpdated(
            new SwimlaneResponse(newSwimlane), updatedAt, cancellationToken);
        return Ok(new MessageResponse("Successfully updated swimlane!"));
    }

    [HttpDelete("{swimlaneId}")] 
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> DeleteSwimlaneAsync(
        ObjectId boardId,
        ObjectId swimlaneId,
        [FromHeader(Name = "If-Match")] DateTime? ifMatch,
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        DateTime updatedAt = await _swimlaneService.DeleteSwimlaneAsync(boardId,
            swimlaneId, userId, ifMatch.GetValueOrDefault(), cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SwimlaneDeleted(swimlaneId, updatedAt, cancellationToken);
        return Ok(new MessageResponse("Swimlane successfully deleted!"));
    }
}