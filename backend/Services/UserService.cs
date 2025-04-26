using Boardly.Backend.Entities;
using Boardly.Backend.Exceptions;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Boardly.Backend.Services;

public class UserService(MongoDbProvider mongoDbProvider, ILogger<UserService> logger,
    IPasswordHasher<User> passwordHasher)
{
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;
    private readonly IMongoCollection<User> _usersCollection = mongoDbProvider.GetUsersCollection();
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
            throw new RecordAlreadyExists("User with such nickname exists!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while adding a user.");
            throw new InvalidOperationException("An unexpected error occurred while adding a user.", ex);
        }
    }

    public async Task UpdatePasswordAsync(User user, CancellationToken cancellationToken = default)
    {
        UpdateResult result;
        try
        {
            user.Password = _passwordHasher.HashPassword(user, user.Password);
            user.UpdatedAt = DateTime.UtcNow;
            var filter = Builders<User>.Filter.Eq(u => u.Id, user.Id);
            var update = Builders<User>.Update
                .Set(u => u.Password, user.Password)
                .Set(u => u.UpdatedAt, user.UpdatedAt);
            result = await _usersCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while adding a user.");
            throw new InvalidOperationException("An unexpected error occurred while adding a user.", ex);
        }
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("User has not been found.");
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
                    Builders<User>.Update
                        .Set(u => u.Password, _passwordHasher.HashPassword(user, providedPassword))
                        .Set(u => u.UpdatedAt, DateTime.UtcNow),
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
}
