using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Boardly.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class BoardController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAllBoards()
    {
        return Ok();
    }

    [HttpGet("{boardId}")]
    [Authorize]
    public async Task<IActionResult> GetBoardById(string boardId)
    {
        return Ok();
    }
}
