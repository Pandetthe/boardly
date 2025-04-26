using Boardly.Backend.Models.Responses;
using Boardly.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Backend.Controllers;

[ApiController]
[Authorize]
[Route("boards/{boardId}/swimlanes/{swimlaneId}/lists")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public class ListController(ListService listService) : ControllerBase
{
    private readonly ListService _listService = listService;

    [HttpGet]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(List<ListResponse>), StatusCodes.Status200OK, "application/json")]
    public Task<IActionResult> GetAllListsAsync(ObjectId boardId, ObjectId swimlaneId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{listId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ListResponse), StatusCodes.Status200OK, "application/json")]
    public Task<IActionResult> GetListByIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{listId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK, "application/json")]
    public Task<IActionResult> CreateListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpDelete("{listId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> DeleteListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await _listService.DeleteListWithRoleCheckAsync(boardId, swimlaneId, listId, userId, cancellationToken);
        return Ok(new MessageResponse("List successfully deleted!"));
    }
}
