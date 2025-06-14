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
[Route("boards/{boardId}/swimlanes/{swimlaneId}/tags"), Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public class TagController : ControllerBase
{
    private readonly TagService _tagService;
    private readonly BoardService _boardService;
    private readonly IHubContext<BoardHub, IBoardClient> _boardHubContext;

    public TagController(TagService tagService, BoardService boardService,
        IHubContext<BoardHub, IBoardClient> boardHubContext)
    {
        _tagService = tagService;
        _boardService = boardService;
        _boardHubContext = boardHubContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TagResponse>), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetAllTagsAsync(ObjectId boardId, ObjectId swimlaneId, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        if (await _boardService.GetUserBoardRoleAsync(boardId, userId, null, cancellationToken) == null)
            throw new ForbiddenException("User is not a member of this board.");
        var tags = await _tagService.GetTagsBySwimlaneIdAsync(boardId, swimlaneId).ToListAsync(cancellationToken);
        return Ok(tags.Select(x => new TagResponse(x)));
    }

    [HttpGet("{tagId}")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetTagByIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId tagId, CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        if (await _boardService.GetUserBoardRoleAsync(boardId, userId, null, cancellationToken) == null)
            throw new ForbiddenException("User is not a member of this board.");
        var tag = await _tagService.GetTagsBySwimlaneIdAsync(boardId, swimlaneId).FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken)
            ?? throw new RecordDoesNotExist("Tag has not been found.");
        return Ok(new TagResponse(tag));
    }

    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(IdResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> CreateTagAsync(
        ObjectId boardId,
        ObjectId swimlaneId,
        [FromBody] CreateTagRequest data,
        [FromHeader(Name = "If-Match")] DateTime? ifMatch, 
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        var tag = new Tag
        {
            Title = data.Title,
            Color = data.Color
        };
        (Tag newTag, DateTime updatedAt) = await _tagService.CreateAndFindTagAsync(boardId,
            swimlaneId, userId, tag, ifMatch.GetValueOrDefault(), cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).TagCreated(swimlaneId, new TagResponse(newTag), updatedAt, cancellationToken);
        return Ok(new IdResponse(tag.Id));
    }

    [HttpPatch("{tagId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> UpdateTagAsync(
        ObjectId boardId,
        ObjectId swimlaneId,
        ObjectId tagId,
        [FromBody] CreateTagRequest data,
        [FromHeader(Name = "If-Match")] DateTime? ifMatch,
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        var tag = new Tag
        {
            Id = tagId,
            Title = data.Title,
            Color = data.Color,
        };
        (Tag newTag, DateTime updatedAt) = await _tagService.UpdateAndFindTagAsync(boardId,
            swimlaneId, userId, tag, ifMatch.GetValueOrDefault(), cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).TagUpdated(swimlaneId, new TagResponse(newTag), updatedAt, cancellationToken);
        return Ok(new MessageResponse("Successfully updated tag!"));
    }

    [HttpDelete("{tagId}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> DeleteTagAsync(
        ObjectId boardId,
        ObjectId swimlaneId,
        ObjectId tagId,
        [FromHeader(Name = "If-Match")] DateTime? ifMatch, 
        CancellationToken cancellationToken)
    {
        ObjectId userId = User.GetUserId();
        DateTime updatedAt = await _tagService.DeleteTagAsync(boardId, swimlaneId, tagId, userId, ifMatch.GetValueOrDefault(), cancellationToken);
        await _boardHubContext.Clients.Group(boardId.ToString()).TagDeleted(swimlaneId, tagId, updatedAt, cancellationToken);
        return Ok(new MessageResponse("Tag successfully deleted!"));
    }
}
