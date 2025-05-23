﻿using Boardly.Api.Entities;
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
[Route("users"), Authorize]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public class UserController(UserService userService) : ControllerBase
{
    private readonly UserService _userService = userService;

    [HttpGet("Me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> GetMeAsync(CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        User user = await _userService.GetUserByIdAsync(userId, cancellationToken)
            ?? throw new Exception("User of logged person has not been found!");
        return Ok(new UserResponse(user));
    }

    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetUserByIdAsync(ObjectId userId)
    {
        User user = await _userService.GetUserByIdAsync(userId)
            ?? throw new RecordDoesNotExist("User has not been found!");
        return Ok(new UserResponse(user));
    }

    [HttpPatch("Me")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdatePasswordAsync([FromBody] UpdatePasswordRequest data, CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = new User
        {
            Id = userId,
            Password = data.Password,
        };
        await _userService.UpdatePasswordAsync(user, cancellationToken);
        return Ok(new MessageResponse("Successfully updated user password!"));
    }
}
