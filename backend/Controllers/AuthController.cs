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
public class AuthController(UserService userService, TokenService tokenService) : ControllerBase
{
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
            ModelState.AddModelError("Nickname", "User with provided nickname already exists!");
            return ValidationProblem(ModelState);
        }
    }

    [HttpPost("Refresh")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshAsync([FromBody] RefreshRequest data, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _tokenService.GetUserByRefreshTokenAsync(data.RefreshToken, cancellationToken);
        if (user == null)
            return Unauthorized(new MessageResponse("Invalid or expired refresh token."));

        (string accessToken, DateTime accessTokenExpiresAt, string refreshToken, DateTime refreshTokenExpiresAt)
            = await _tokenService.GenerateAndStoreTokensAsync(user, cancellationToken);

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

    private static uint CalculateExpiryInSeconds(DateTime expiresAt) => (uint)Math.Ceiling(Math.Max(0, (expiresAt - DateTime.UtcNow).TotalMilliseconds) / 1000);
}
