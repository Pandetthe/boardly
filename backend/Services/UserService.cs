using Boardly.Backend.Entities;
using Boardly.Backend.Exceptions;
using Microsoft.AspNetCore.Identity;
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
            var cursor = await _usersCollection.FindAsync(u => u.Nickname == nickname, null, cancellationToken);
            return await cursor.FirstOrDefaultAsync(cancellationToken);
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

    public async Task InitAsync(CancellationToken cancellationToken = default)
    {
        CreateIndexModel<User> textIndex = new(
            Builders<User>.IndexKeys.Text(u => u.Nickname),
            new() { Name = "unique_text_nickname", Unique = true }
        );
        await _usersCollection.Indexes.CreateOneAsync(textIndex, null, cancellationToken);
    }
}
