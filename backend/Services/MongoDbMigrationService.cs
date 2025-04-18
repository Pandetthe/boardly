using Boardly.Backend.Entities;
using MongoDB.Driver;

namespace Boardly.Backend.Services;

public class MongoDbMigrationService(MongoDbProvider mongoDbProvider) : IHostedService
{
    private readonly MongoDbProvider _mongoDbProvider = mongoDbProvider;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        CreateIndexModel<User> textIndex = new(
            Builders<User>.IndexKeys.Text(u => u.Nickname),
            new() { Name = "unique_text_nickname", Unique = true }
        );

        await _mongoDbProvider.GetUsersCollection().Indexes.CreateOneAsync(textIndex, null, cancellationToken);

        var uniqueTokenIndex = new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.Token),
            new CreateIndexOptions { Unique = true, Name = "unique_refresh_token" }
        );

        await _mongoDbProvider.GetRefreshTokensCollection().Indexes.CreateOneAsync(uniqueTokenIndex, null, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
