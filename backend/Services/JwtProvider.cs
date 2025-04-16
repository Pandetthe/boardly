using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Boardly.Backend.Entities;

namespace Boardly.Backend.Services;

public class JwtProvider(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public async Task GenerateToken(User user)
    {
        var claims = new[]
        {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Nickname, user.Nickname)
            };

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]
            ?? throw new NullReferenceException("Jwt key must be provided!")));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration.GetValue<string>("Jwt:Issuer"),
            audience: _configuration.GetValue<string>("Jwt:Audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),//_configuration.GetValue<string>("Jwt:AccessTokenExpirationInMinutes")),
            signingCredentials: creds
        );

        string jwt = new JwtSecurityTokenHandler().WriteToken(token);
    }
}
