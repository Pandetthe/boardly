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
    private readonly ILogger<UserService> _logger = logger;

    public async Task InsertUserAsync(User user, CancellationToken cancellationToken = default)
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

    public async Task<User?> SelectUserAsync(string nickname, CancellationToken cancellationToken = default)
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

    public async Task AddRefreshToken(ObjectId userId, RefreshToken token, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var removeExpiredUpdate = Builders<User>.Update
                .PullFilter(u => u.RefreshTokens, rt => rt.ExpiresAtUtc < DateTime.UtcNow);

            await _usersCollection.UpdateOneAsync(filter, removeExpiredUpdate, null, cancellationToken);

            var addNewTokenUpdate = Builders<User>.Update
                .Push(u => u.RefreshTokens, token);

            await _usersCollection.UpdateOneAsync(filter, addNewTokenUpdate, null, cancellationToken);
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
            var filter = Builders<User>.Filter.ElemMatch(u => u.RefreshTokens, rt =>
                rt.Token == refreshToken && rt.ExpiresAtUtc > DateTime.UtcNow);

            var user = await _usersCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            if (user != null) {
                var pullOldToken = Builders<User>.Update.PullFilter(u => u.RefreshTokens, rt => rt.Token == refreshToken);
                await _usersCollection.UpdateOneAsync(u => u.Id == user.Id, pullOldToken, cancellationToken: cancellationToken);
            }
            return user;
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

        var refreshTokenIndex = new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending("RefreshTokens.Token"),
            new CreateIndexOptions { Name = "refresh_token_index" }
        );

        await _usersCollection.Indexes.CreateManyAsync([textIndex, refreshTokenIndex], cancellationToken);
    }
}
