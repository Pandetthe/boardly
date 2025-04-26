using Boardly.Backend.Entities;
using Boardly.Backend.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace Boardly.Backend.Controllers;

[ApiController]
[Authorize]
[Route("boards/{boardId}/swimlanes")]
[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(MessageResponse), StatusCodes.Status404NotFound)]
public class SwimlaneController : ControllerBase
{
    [HttpGet]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(List<SwimlaneResponse>), StatusCodes.Status200OK, "application/json")]
    public Task<IActionResult> GetAllSwimlanesAsync(ObjectId boardId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{swimlaneId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(SwimlaneResponse), StatusCodes.Status200OK, "application/json")]
    public Task<IActionResult> GetSwimlaneByIdAsync(ObjectId boardId, ObjectId swimlaneId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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
    public Task<IActionResult> DeleteSwimlaneAsync(ObjectId boardId, ObjectId swimlaneId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}