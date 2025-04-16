﻿using Boardly.Backend.Entities;
using Boardly.Backend.Exceptions;
using Boardly.Backend.Models;
using Boardly.Backend.Models.Auth;
using Boardly.Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Boardly.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly UserService _userService;
    private readonly JwtProvider _jwtProvider;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger, UserService userService, JwtProvider jwtProvider)
    {
        _configuration = configuration;
        _logger = logger;
        _userService = userService;
        _jwtProvider = jwtProvider;
    }

    [HttpPost("SignIn")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SignInResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult>SignIn([FromBody] SignInRequest data, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        User? user = await _userService.SelectUserAsync(data.Nickname, cancellationToken);
        if (user != null)
        {
            if (await _userService.VerifyHashedPassword(user, data.Password, cancellationToken))
            {
                string token = _jwtProvider.GenerateToken(user);
                return Ok(new SignInResponse(token, 3600, "example"));
            }
        }
        return Unauthorized(new MessageResponse("Invalid credentials"));
    }

    [HttpPost("SignUp")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SignUpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult>SignUp([FromBody] SignUpRequest data, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            User user = new()
            {
                Nickname = data.Nickname,
                Password = data.Password
            };
            await _userService.InsertUserAsync(user, cancellationToken);
            string token = _jwtProvider.GenerateToken(user);
            return Ok(new SignUpResponse(token, 3600, "example"));
        }
        catch (RecordAlreadyExists)
        {
            ModelState.AddModelError("Email", "User with provided email already exists!");
            return ValidationProblem(ModelState);
        }
    }

    [HttpPost("Refresh")]
    public async Task<IActionResult>Refresh()
    {
        return Ok();
    }
}
