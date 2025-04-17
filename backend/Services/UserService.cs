using Boardly.Backend.Entities;
using Boardly.Backend.Exceptions;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Boardly.Backend.Services;

public class UserService(MongoDbProvider mongoDbProvider, ILogger<UserService> logger,
    IPasswordHasher<User> passwordHasher) : IDbInitializator
{
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
    private readonly IMongoCollection<User> _usersCollection = mongoDbProvider.GetUsersCollection();
    private readonly IMongoCollection<RefreshToken> _refreshTokensCollection = mongoDbProvider.GetRefreshTokensCollection();
    private readonly ILogger<UserService> _logger = logger;

    public async Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        try
        {
            user.Password = _passwordHasher.HashPassword(user, user.Password);
            await _usersCollection.InsertOneAsync(user, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new RecordAlreadyExists();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while adding a user.");
            throw new InvalidOperationException("An unexpected error occurred while adding a user.", ex);
        }
    }

    public async Task<User?> GetUserByNicknameAsync(string nickname, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _usersCollection.Find(u => u.Nickname == nickname, null).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while selecting the user.");
            throw new InvalidOperationException("An unexpected error occurred while selecting the user.", ex);
        }
    }

    public async Task<User?> GetUserByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _usersCollection.Find(u => u.Id == id, null).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while selecting the user.");
            throw new InvalidOperationException("An unexpected error occurred while selecting the user.", ex);
        }
    }

    public async Task<bool> VerifyHashedPassword(User user, string providedPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = _passwordHasher.VerifyHashedPassword(user, user.Password, providedPassword);
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                await _usersCollection.UpdateOneAsync(
                    u => u.Id == user.Id,
                    Builders<User>.Update.Set(u => u.Password, _passwordHasher.HashPassword(user, providedPassword)),
                    null,
                    cancellationToken
                );
            }
            return result != PasswordVerificationResult.Failed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while verifying hashed password.");
            throw new InvalidOperationException("An unexpected error occurred while verifying hashed password.", ex);
        }
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
            //await _usersCollection.UpdateOneAsync(filter, addNewTokenUpdate, null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while adding refresh token.");
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

            return await _usersCollection.Find(u => u.Id == token.UserId).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while getting user by refresh token.");
            throw new InvalidOperationException("An unexpected error occurred while validating refresh token.", ex);
        }
    }

    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        CreateIndexModel<User> textIndex = new(
            Builders<User>.IndexKeys.Text(u => u.Nickname),
            new() { Name = "unique_text_nickname", Unique = true }
        );

        await _usersCollection.Indexes.CreateOneAsync(textIndex, null, cancellationToken);

        var uniqueTokenIndex = new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.Token),
            new CreateIndexOptions { Unique = true, Name = "unique_refresh_token" }
        );

        await _refreshTokensCollection.Indexes.CreateOneAsync(uniqueTokenIndex, null, cancellationToken);
    }
}
