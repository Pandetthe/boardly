using Boardly.Backend.Entities;
using Boardly.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController(UserService userService) : ControllerBase
{
    private readonly UserService _userService = userService;

    [HttpGet("Me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        User? user = await _userService.GetUserByIdAsync(new ObjectId(User.FindFirst(ClaimTypes.NameIdentifier)!.Value));
        return Ok(user);
    }

    [HttpGet("{userId}")]
    [Authorize]
    public async Task<IActionResult> GetUserById(string userId)
    {
        return Ok();
    }
}
