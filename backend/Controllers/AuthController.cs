using Boardly.Backend.Entities;
using Boardly.Backend.Exceptions;
using Boardly.Backend.Models;
using Boardly.Backend.Models.Auth;
using Boardly.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(ILogger<AuthController> logger, UserService userService, TokenService tokenService) : ControllerBase
{
    private readonly ILogger<AuthController> _logger = logger;
    private readonly UserService _userService = userService;
    private readonly TokenService _tokenService = tokenService;

    [HttpPost("SignIn")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult>SignInAsync([FromBody] SignInRequest data, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        User? user = await _userService.GetUserByNicknameAsync(data.Nickname, cancellationToken);
        if (user != null)
        {
            if (await _userService.VerifyHashedPassword(user, data.Password, cancellationToken))
            {
                (string accessToken, DateTime accessTokenExpiresAt, string refreshToken, DateTime refreshTokenExpiresAt) = await AddNewRefreshToken(user, cancellationToken);
                return Ok(new AuthResponse(
                    accessToken, 
                    CalculateExpiryInSeconds(accessTokenExpiresAt),
                    refreshToken, 
                    CalculateExpiryInSeconds(refreshTokenExpiresAt)
                ));
            }
        }
        return Unauthorized(new MessageResponse("Invalid credentials"));
    }

    [HttpPost("SignUp")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SignUpAsync([FromBody] SignUpRequest data, CancellationToken cancellationToken)
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
            await _userService.AddUserAsync(user, cancellationToken);
            (string accessToken, DateTime accessTokenExpiresAt, string refreshToken, DateTime refreshTokenExpiresAt) = await AddNewRefreshToken(user, cancellationToken);
            return Ok(new AuthResponse(
                accessToken,
                CalculateExpiryInSeconds(accessTokenExpiresAt),
                refreshToken,
                CalculateExpiryInSeconds(refreshTokenExpiresAt)
            ));
        }
        catch (RecordAlreadyExists)
        {
            ModelState.AddModelError("Email", "User with provided email already exists!");
            return ValidationProblem(ModelState);
        }
    }

    [HttpPost("Refresh")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshAsync([FromBody] Microsoft.AspNetCore.Identity.Data.RefreshRequest data, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _tokenService.GetUserByRefreshTokenAsync(data.RefreshToken, cancellationToken);
        if (user == null)
            return Unauthorized(new MessageResponse("Invalid or expired refresh token."));

        (string accessToken, DateTime accessTokenExpiresAt, string refreshToken, DateTime refreshTokenExpiresAt) = await AddNewRefreshToken(user, cancellationToken);

        return Ok(new AuthResponse(
            accessToken,
            CalculateExpiryInSeconds(accessTokenExpiresAt),
            refreshToken,
            CalculateExpiryInSeconds(refreshTokenExpiresAt)
        ));
    }

    [HttpPost("Revoke")]
    [Authorize]
    [Produces("application/json")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeAsync(CancellationToken cancellationToken)
    {
        await _tokenService.DeleteAllRefreshTokens(new ObjectId(User.FindFirst(ClaimTypes.NameIdentifier)!.Value), cancellationToken);
        return Ok(new MessageResponse("Successfully revoked all refresh tokens."));
    }

    private async Task<(string, DateTime, string, DateTime)> AddNewRefreshToken(User user, CancellationToken cancellationToken)
    {
        for (int i = 0; i < 3; i++) // Kinda overkill
        {
            try
            {
                (string accessToken, DateTime accessTokenExpiresAt, string refreshToken, DateTime refreshTokenExpiresAt) = _tokenService.GenerateTokens(user);
                RefreshToken refreshTokenData = new()
                {
                    UserId = user.Id,
                    ExpiresAt = refreshTokenExpiresAt,
                    Token = refreshToken
                };
                await _tokenService.AddRefreshToken(refreshTokenData, cancellationToken);
                return (accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt);
            }
            catch (RecordAlreadyExists) { /* RETRY */ }
        }
        throw new Exception("Failed to generate a unique refresh token after 3 attempts.");
    }

    private static uint CalculateExpiryInSeconds(DateTime expiresAt) => (uint)Math.Ceiling(Math.Max(0, (expiresAt - DateTime.UtcNow).TotalMilliseconds) / 1000);
}
