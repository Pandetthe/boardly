using Boardly.Backend.Entities;
using Boardly.Backend.Exceptions;
using Boardly.Backend.Models;
using Boardly.Backend.Models.Auth;
using Boardly.Backend.Services;
using Microsoft.AspNetCore.Identity.Data;
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
                (string accessToken, DateTime accessTokenExpiresAt, string refreshToken, DateTime refreshTokenExpiresAt) = _jwtProvider.GenerateTokens(user);
                RefreshToken refreshTokenData = new()
                {
                    UserId = user.Id,
                    ExpiresAt = refreshTokenExpiresAt,
                    Token = refreshToken
                };
                await _userService.AddRefreshToken(refreshTokenData, cancellationToken);
    
                return Ok(new SignInResponse(accessToken, CalculateExpiryInSeconds(accessTokenExpiresAt), refreshToken, CalculateExpiryInSeconds(refreshTokenExpiresAt)));
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
            (string accessToken, DateTime accessTokenExpiresAt, string refreshToken, DateTime refreshTokenExpiresAt) = _jwtProvider.GenerateTokens(user);
            RefreshToken refreshTokenData = new()
            {
                UserId = user.Id,
                ExpiresAt = refreshTokenExpiresAt,
                Token = refreshToken
            };
            await _userService.AddRefreshToken(refreshTokenData, cancellationToken);
            return Ok(new SignUpResponse(accessToken, CalculateExpiryInSeconds(accessTokenExpiresAt), refreshToken, CalculateExpiryInSeconds(refreshTokenExpiresAt)));
        }
        catch (RecordAlreadyExists)
        {
            ModelState.AddModelError("Email", "User with provided email already exists!");
            return ValidationProblem(ModelState);
        }
    }

    [HttpPost("Refresh")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(SignInResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _userService.GetUserByRefreshTokenAsync(request.RefreshToken, cancellationToken);
        if (user == null)
            return Unauthorized(new MessageResponse("Invalid or expired refresh token."));

        (string newAccessToken, DateTime accessExpires, string newRefreshToken, DateTime refreshExpires) = _jwtProvider.GenerateTokens(user);
        await _userService.AddRefreshToken(new()
        {
            UserId = user.Id,
            Token = newRefreshToken,
            ExpiresAt = refreshExpires
        }, cancellationToken);

        return Ok(new SignInResponse(
            newAccessToken,
            CalculateExpiryInSeconds(accessExpires),
            newRefreshToken,
            CalculateExpiryInSeconds(refreshExpires)
        ));
    }

    private static uint CalculateExpiryInSeconds(DateTime expiresAt) => (uint)Math.Ceiling(Math.Max(0, (expiresAt - DateTime.UtcNow).TotalSeconds) / 60) * 60;
}
