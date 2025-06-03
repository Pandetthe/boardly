using Boardly.Api.Entities;
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
[Route("auth")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError, "application/problem+json")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly TokenService _tokenService;

    public AuthController(UserService userService, TokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    [HttpPost("signin")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> SignInAsync([FromBody] SignInRequest data, CancellationToken cancellationToken)
    {
        User? user = await _userService.GetUserByNicknameAsync(data.Nickname, cancellationToken);
        if (user != null)
        {
            if (await _userService.VerifyHashedPassword(user, data.Password, cancellationToken))
            {
                (string accessToken, DateTime accessTokenExpiresAt, string refreshToken, DateTime refreshTokenExpiresAt)
                    = await _tokenService.GenerateAndStoreTokensAsync(user, cancellationToken);
                return Ok(new AuthResponse(
                    accessToken,
                    CalculateExpiryInSeconds(accessTokenExpiresAt),
                    refreshToken,
                    CalculateExpiryInSeconds(refreshTokenExpiresAt)
                ));
            }
        }
        throw new UnauthorizedException("Invalid credentials.");
    }

    [HttpPost("signup")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> SignUpAsync([FromBody] SignUpRequest data, CancellationToken cancellationToken)
    {
        try
        {
            User user = new()
            {
                Nickname = data.Nickname,
                Password = data.Password
            };
            await _userService.AddUserAsync(user, cancellationToken);
            (string accessToken, DateTime accessTokenExpiresAt, string refreshToken, DateTime refreshTokenExpiresAt)
                = await _tokenService.GenerateAndStoreTokensAsync(user, cancellationToken);
            return Ok(new AuthResponse(
                accessToken,
                CalculateExpiryInSeconds(accessTokenExpiresAt),
                refreshToken,
                CalculateExpiryInSeconds(refreshTokenExpiresAt)
            ));
        }
        catch (RecordAlreadyExists)
        {
            ModelState.AddModelError(nameof(SignUpRequest.Nickname), "User with provided nickname already exists!");
            return ValidationProblem(ModelState);
        }
    }

    [HttpPost("refresh")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<IActionResult> RefreshAsync([FromBody] RefreshRequest data, CancellationToken cancellationToken)
    {
        var user = await _tokenService.GetUserByRefreshTokenAsync(data.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        (string accessToken, DateTime accessTokenExpiresAt, string refreshToken, DateTime refreshTokenExpiresAt)
            = await _tokenService.GenerateAndStoreTokensAsync(user, cancellationToken);

        return Ok(new AuthResponse(
            accessToken,
            CalculateExpiryInSeconds(accessTokenExpiresAt),
            refreshToken,
            CalculateExpiryInSeconds(refreshTokenExpiresAt)
        ));
    }

    [HttpPost("revoke"), Authorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK, "application/json")]
    public async Task<IActionResult> RevokeAsync(CancellationToken cancellationToken)
    {
        ObjectId userId = ObjectId.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await _tokenService.DeleteAllRefreshTokens(userId, cancellationToken);
        return Ok(new MessageResponse("Successfully revoked all refresh tokens."));
    }

    private static uint CalculateExpiryInSeconds(DateTime expiresAt) => (uint)Math.Ceiling(Math.Max(0, (expiresAt - DateTime.UtcNow).TotalMilliseconds) / 1000);
}
