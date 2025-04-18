using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Boardly.Backend.Entities;
using System.Security.Cryptography;

namespace Boardly.Backend.Services;

public class JwtProvider(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public (string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt) GenerateTokens(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Nickname, user.Nickname)
        };

        var key = new SymmetricSecurityKey( Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]
                ?? throw new NullReferenceException("Jwt key must be provided!")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        int accessTokenExpiresInMinutes = _configuration.GetValue("Jwt:AccessTokenExpiresInMinutes", 15);
        int refreshTokenExpiresInDays = _configuration.GetValue("Jwt:RefreshTokenExpiresInDays", 7);
        DateTime accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenExpiresInMinutes);
        DateTime refreshTokenExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiresInDays);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: accessTokenExpiresAt,
            signingCredentials: creds
        );

        string accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        string refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return (accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt);
    }
}
