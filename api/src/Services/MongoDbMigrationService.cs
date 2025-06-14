using Boardly.Api.Entities;
using Boardly.Api.Entities.Board;
using MongoDB.Driver;

namespace Boardly.Api.Services;

public class MongoDbMigrationService(MongoDbProvider mongoDbProvider) : IHostedService
{
    private readonly MongoDbProvider _mongoDbProvider = mongoDbProvider;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        CreateIndexModel<User> textUserIndex = new(
            Builders<User>.IndexKeys.Text(u => u.Nickname),
            new() { Name = "unique_text_nickname", Unique = true }
        );

        await _mongoDbProvider.GetUsersCollection().Indexes.CreateOneAsync(textUserIndex, null, cancellationToken);

        var uniqueTokenIndex = new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.Token),
            new CreateIndexOptions { Unique = true, Name = "unique_refresh_token" }
        );

        var ttlIndex = new CreateIndexModel<RefreshToken>(
            Builders<RefreshToken>.IndexKeys.Ascending(rt => rt.ExpiresAt),
            new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ttl" }
        );

        await _mongoDbProvider.GetRefreshTokensCollection().Indexes.CreateManyAsync([uniqueTokenIndex, ttlIndex], null, cancellationToken);

        CreateIndexModel<Board> textBoardIndex = new(
            Builders<Board>.IndexKeys.Text(u => u.Title),
            new() { Name = "text_title" }
        );

        await _mongoDbProvider.GetBoardsCollection().Indexes.CreateOneAsync(textBoardIndex, null, cancellationToken);

        CreateIndexModel<Card> cardIndex = new(
            Builders<Card>.IndexKeys.Ascending(c => c.BoardId)
                .Ascending(c => c.ListId)
                .Ascending(c => c.SwimlaneId),
            new() { Name = "board_list_swimlane" }
        );

        await _mongoDbProvider.GetCardsCollection().Indexes.CreateOneAsync(cardIndex, null, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
