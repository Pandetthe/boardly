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

namespace Boardly.Api.Controllers;

[ApiController]
[Route("boards/{boardId}/swimlanes/{swimlaneId}/tags"), Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public class TagController : ControllerBase
{
    private readonly TagService _tagService;
    private readonly IHubContext<BoardHub> _boardHubContext;

    public TagController(TagService tagService, IHubContext<BoardHub> boardHubContext)
    {
        _tagService = tagService;
        _boardHubContext = boardHubContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TagResponse>), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetAllTagsAsync(ObjectId boardId, ObjectId swimlaneId, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        var tags = await _tagService.GetTagsBySwimlaneIdAsync(boardId, swimlaneId, userId, cancellationToken);
        return Ok(tags.Select(x => new TagResponse(x)));
    }

    [HttpGet("{tagId}")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetTagByIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId tagId, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        var tag = await _tagService.GetTagByIdAsync(boardId, swimlaneId, tagId, userId, cancellationToken)
            ?? throw new RecordDoesNotExist("Tag has not been found.");
        return Ok(new TagResponse(tag));
    }

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> CreateTagAsync(ObjectId boardId, ObjectId swimlaneId, [FromBody] CreateTagRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        var tag = new Tag
        {
            Title = data.Title,
            Color = data.Color
        };
        await _tagService.CreateTagAsync(boardId, swimlaneId, userId, tag, cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SendAsync("TagCreate", cancellationToken);
        return Ok(new IdResponse(tag.Id));
    }

    [HttpPatch("{tagId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> UpdateTagAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId tagId, [FromBody] CreateTagRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        var tag = new Tag
        {
            Id = tagId,
            Title = data.Title,
            Color = data.Color,
        };
        await _tagService.UpdateTagAsync(boardId, swimlaneId, userId, tag, cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SendAsync("TagUpdate", cancellationToken);
        return Ok(new MessageResponse("Successfully updated tag!"));
    }

    [HttpDelete("{tagId}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> DeleteTagAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId tagId, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        await _tagService.DeleteTagAsync(boardId, swimlaneId, tagId, userId, cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).SendAsync("TagDelete", cancellationToken);
        return Ok(new MessageResponse("Tag successfully deleted!"));
    }
}
