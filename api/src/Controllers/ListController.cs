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
[Route("boards/{boardId}/swimlanes/{swimlaneId}/lists"), Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public class ListController : ControllerBase
{
    private readonly ListService _listService;
    private readonly BoardService _boardService;
    private readonly IHubContext<BoardHub, IBoardClient> _boardHubContext;

    public ListController(ListService listService, BoardService boardService,
        IHubContext<BoardHub, IBoardClient> boardHubContext)
    {
        _listService = listService;
        _boardService = boardService;
        _boardHubContext = boardHubContext;
    }

    [HttpGet]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(List<ListResponse>), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetAllListsAsync(
        [FromRoute] ObjectId boardId,
        [FromRoute] ObjectId swimlaneId,
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        if (await _boardService.GetUserBoardRoleAsync(boardId, userId, cancellationToken) == null)
            throw new ForbiddenException("User is not a member of this board.");
        List<List> swimlanes = await _listService.GetListBySwimlaneId(boardId, swimlaneId).ToListAsync(cancellationToken);
        return Ok(swimlanes.Select(x => new ListResponse(x)).ToList());
    }

    [HttpGet("{listId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(DetailedListResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetListByIdAsync(
        [FromRoute] ObjectId boardId,
        [FromRoute] ObjectId swimlaneId,
        [FromRoute] ObjectId listId,
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        if (await _boardService.GetUserBoardRoleAsync(boardId, userId, cancellationToken) == null)
            throw new ForbiddenException("User is not a member of this board.");
        List list = await _listService.GetListBySwimlaneId(boardId, swimlaneId).FirstOrDefaultAsync(l => l.Id == listId, cancellationToken)
            ?? throw new RecordDoesNotExist("List has not been found.");
        return Ok(new DetailedListResponse(list));
    }

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> CreateListAsync(
        [FromRoute] ObjectId boardId,
        [FromRoute] ObjectId swimlaneId,
        [FromBody] CreateUpdateListRequest data,
        [FromHeader(Name = "If-Match")] DateTime? ifMatch,
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        var list = new List
        {
            Title = data.Title,
            MaxWIP = data.MaxWIP,
            Color = data.Color
        };

        (List newList, DateTime updatedAt) = await _listService.CreateAndFindListAsync(boardId,
            swimlaneId, userId, list, ifMatch.GetValueOrDefault(), cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).ListCreated(
            swimlaneId, new ListResponse(newList), updatedAt, cancellationToken);
        return Ok(new IdResponse(list.Id));
    }

    [HttpPatch("{listId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> UpdateListAsync(
        [FromRoute] ObjectId boardId,
        [FromRoute] ObjectId swimlaneId,
        [FromRoute] ObjectId listId,
        [FromBody] CreateUpdateListRequest data,
        [FromHeader(Name = "If-Match")] DateTime? ifMatch,
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        var list = new List
        {
            Id = listId,
            Title = data.Title,
            MaxWIP = data.MaxWIP,
            Color = data.Color
        };

        (List newList, DateTime updatedAt) = await _listService.UpdateAndFindListAsync(boardId,
            swimlaneId, userId, list, ifMatch.GetValueOrDefault(), cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).ListUpdated(
            swimlaneId, new ListResponse(newList), updatedAt, cancellationToken);
        return Ok(new MessageResponse("Successfully updated list!"));
    }

    [HttpDelete("{listId}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> DeleteListAsync(
        [FromRoute] ObjectId boardId,
        [FromRoute] ObjectId swimlaneId,
        [FromRoute] ObjectId listId,
        [FromHeader(Name = "If-Match")] DateTime? ifMatch,
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        DateTime updatedAt = await _listService.DeleteListAsync(boardId, swimlaneId,
            listId, userId, ifMatch.GetValueOrDefault(), cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).ListDeleted(swimlaneId, listId, updatedAt, cancellationToken);
        return Ok(new MessageResponse("List successfully deleted!"));
    }
}
