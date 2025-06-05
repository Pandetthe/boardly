using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using Boardly.Api.Models.Requests;
using Boardly.Api.Models.Responses;
using Boardly.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

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

    public SwimlaneController(SwimlaneService swimlaneService)
    {
        _swimlaneService = swimlaneService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<SwimlaneResponse>), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetAllSwimlanesAsync(ObjectId boardId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        List<Swimlane> swimlanes = [.. await _swimlaneService.GetSwimlanesByBoardIdAsync(boardId, userId, cancellationToken)];
        return Ok(swimlanes.Select(x => new SwimlaneResponse(x)).ToList());
    }

    [HttpGet("{swimlaneId}")]
    [ProducesResponseType(typeof(DetailedSwimlaneResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetSwimlaneByIdAsync(ObjectId boardId, ObjectId swimlaneId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Swimlane swimlane = await _swimlaneService.GetSwimlaneByIdAsync(boardId, swimlaneId, userId, cancellationToken)
            ?? throw new RecordDoesNotExist("Swimlane has not been found.");
        return Ok(new DetailedSwimlaneResponse(swimlane));
    }


    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> CreateSwimlaneAsync(ObjectId boardId, [FromBody] CreateSwimlaneRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
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

        await _swimlaneService.CreateSwimlaneAsync(boardId, userId, swimlane, cancellationToken);
        return Ok(new IdResponse(swimlane.Id));
    }

    [HttpPatch("{swimlaneId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> UpdateSwimlaneAsync(ObjectId boardId, ObjectId swimlaneId, [FromBody] CreateSwimlaneRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var swimlane = new Swimlane
        {
            Id = swimlaneId,
            Title = data.Title,
            Tags = [.. data.Tags?.Select(tag => new Tag
            {
                Title = tag.Title,
                Color = tag.Color,
            }) ?? []],
            Lists = [.. data.Lists?.Select(list => new List
            {
                Title = list.Title,
                MaxWIP = list.MaxWIP,
            }) ?? []],
        };

        await _swimlaneService.UpdateSwimlaneAsync(boardId, userId, swimlane, cancellationToken);
        return Ok(new MessageResponse("Successfully updated swimlane!"));
    }

    [HttpDelete("{swimlaneId}")] 
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> DeleteSwimlaneAsync(ObjectId boardId, ObjectId swimlaneId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await _swimlaneService.DeleteSwimlaneAsync(boardId, swimlaneId, userId, cancellationToken);
        return Ok(new MessageResponse("Swimlane successfully deleted!"));
    }
}