using Boardly.Backend.Entities;
using Boardly.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Backend.Controllers;

[ApiController]
[Route("users"), Authorize]
public class UserController(UserService userService) : ControllerBase
{
    private readonly UserService _userService = userService;

    [HttpGet("Me")]
    public async Task<IActionResult> GetMe()
    {
        User user = await _userService.GetUserByIdAsync(new ObjectId(User.FindFirst(ClaimTypes.NameIdentifier)!.Value))
            ?? throw new InvalidOperationException("User not found");
        return Ok(user);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserById(ObjectId userId)
    {
        User? user = await _userService.GetUserByIdAsync(userId);
        return Ok(user);
    }
}
