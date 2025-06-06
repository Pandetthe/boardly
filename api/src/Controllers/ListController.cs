using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using Boardly.Api.Hubs;
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
[Route("boards/{boardId}/swimlanes/{swimlaneId}/lists"), Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public class ListController : ControllerBase
{
    private readonly ListService _listService;
    private readonly IHubContext<BoardHub> _boardHubContext;

    public ListController(ListService listService, IHubContext<BoardHub> boardHubContext)
    {
        _listService = listService;
        _boardHubContext = boardHubContext;
    }

    [HttpGet]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(List<ListResponse>), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetAllListsAsync(ObjectId boardId, ObjectId swimlaneId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        List<List> swimlanes = [.. await _listService.GetListsBySwimlaneIdAsync(boardId, swimlaneId, userId, cancellationToken)];
        return Ok(swimlanes.Select(x => new ListResponse(x)).ToList());
    }

    [HttpGet("{listId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(DetailedListResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetListByIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        List list = await _listService.GetListByIdAsync(boardId, swimlaneId, listId, userId, cancellationToken)
            ?? throw new RecordDoesNotExist("List has not been found.");
        return Ok(new DetailedListResponse(list));
    }

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> CreateListAsync(ObjectId boardId, ObjectId swimlaneId, CreateUpdateListRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var list = new List
        {
            Title = data.Title,
            MaxWIP = data.MaxWIP,
            Color = data.Color
        };

        await _listService.CreateListAsync(boardId, swimlaneId, userId, list, cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SendAsync("Update", cancellationToken);
        return Ok(new IdResponse(list.Id));
    }

    [HttpPatch("{listId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> UpdateListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, CreateUpdateListRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var list = new List
        {
            Id = listId,
            Title = data.Title,
            MaxWIP = data.MaxWIP,
            Color = data.Color
        };

        await _listService.UpdateListAsync(boardId, swimlaneId, userId, list, cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SendAsync("Update", cancellationToken);
        return Ok(new MessageResponse("Successfully updated list!"));
    }

    [HttpDelete("{listId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> DeleteListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await _listService.DeleteListAsync(boardId, swimlaneId, listId, userId, cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SendAsync("Update", cancellationToken);
        return Ok(new MessageResponse("List successfully deleted!"));
    }
}
