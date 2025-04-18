using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Boardly.Backend.Entities;
using System.Security.Cryptography;
using MongoDB.Driver;
using Boardly.Backend.Exceptions;
using MongoDB.Bson;

namespace Boardly.Backend.Services;

public class TokenService(MongoDbProvider mongoDbProvider, IConfiguration configuration, ILogger<TokenService> logger, UserService userService)
{
    private readonly IMongoCollection<RefreshToken> _refreshTokensCollection = mongoDbProvider.GetRefreshTokensCollection();
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<TokenService> _logger = logger;
    private readonly UserService _userService = userService;

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

    public async Task AddRefreshToken(RefreshToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteExpiredFilter = Builders<RefreshToken>.Filter.And(
                Builders<RefreshToken>.Filter.Eq(rt => rt.UserId, token.UserId),
                Builders<RefreshToken>.Filter.Lt(rt => rt.ExpiresAt, DateTime.UtcNow)
            );

            await _refreshTokensCollection.DeleteManyAsync(deleteExpiredFilter, cancellationToken);

            await _refreshTokensCollection.InsertOneAsync(token, null, cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new RecordAlreadyExists("Duplicate of refresh tokens occured!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while adding refresh token.");
            throw new InvalidOperationException("An unexpected error occurred while adding refresh token.", ex);
        }
    }

    public async Task DeleteAllRefreshTokens(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteExpiredFilter = Builders<RefreshToken>.Filter.And(
                Builders<RefreshToken>.Filter.Eq(rt => rt.UserId, userId)
            );

            await _refreshTokensCollection.DeleteManyAsync(deleteExpiredFilter, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting refresh tokens.");
            throw new InvalidOperationException("An unexpected error occurred while adding refresh token.", ex);
        }
    }


    public async Task<User?> GetUserByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var tokenFilter = Builders<RefreshToken>.Filter.And(
                Builders<RefreshToken>.Filter.Eq(rt => rt.Token, refreshToken),
                Builders<RefreshToken>.Filter.Gt(rt => rt.ExpiresAt, DateTime.UtcNow)
            );

            var token = await _refreshTokensCollection.Find(tokenFilter).FirstOrDefaultAsync(cancellationToken);

            if (token == null)
                return null;

            var deleteTokenFilter = Builders<RefreshToken>.Filter.Eq(rt => rt.Token, refreshToken);
            var result = await _refreshTokensCollection.DeleteOneAsync(deleteTokenFilter, cancellationToken);

            return await _userService.GetUserByIdAsync(token.UserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while getting user by refresh token.");
            throw new InvalidOperationException("An unexpected error occurred while validating refresh token.", ex);
        }
    }
}
