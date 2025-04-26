using Boardly.Backend.Entities;
using Boardly.Backend.Exceptions;
using Boardly.Backend.Models.Responses;
using Boardly.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Backend.Controllers;

[ApiController]
[Authorize]
[Route("boards/{boardId}/swimlanes")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public class SwimlaneController(SwimlaneService swimlaneService) : ControllerBase
{
    private readonly SwimlaneService _swimlaneService = swimlaneService;

    [HttpGet]
    [ProducesResponseType(typeof(List<SwimlaneResponse>), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetAllSwimlanesAsync(ObjectId boardId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        List<Swimlane> swimlanes = [.. await _swimlaneService.GetSwimlanesByBoardIdAsync(boardId, userId, cancellationToken)];
        return Ok(swimlanes.Select(x => new SwimlaneResponse(x)).ToList());
    }

    [HttpGet("{swimlaneId}")]
    [ProducesResponseType(typeof(SwimlaneResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetSwimlaneByIdAsync(ObjectId boardId, ObjectId swimlaneId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Swimlane swimlane = await _swimlaneService.GetSwimlaneByIdAsync(boardId, swimlaneId, userId, cancellationToken)
            ?? throw new RecordDoesNotExist("Swimlane has not been found.");
        return Ok(new SwimlaneResponse(swimlane));
    }


    [HttpPost("{swimlaneId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK, "application/json")]
    public Task<IActionResult> CreateSwimlaneAsync(ObjectId boardId, ObjectId swimlaneId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("{swimlaneId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> DeleteSwimlaneAsync(ObjectId boardId, ObjectId swimlaneId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await _swimlaneService.DeleteSwimlaneAsync(boardId, swimlaneId, userId, cancellationToken);
        return Ok(new MessageResponse("Swimlane successfully deleted!"));
    }
}