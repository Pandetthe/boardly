using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Boardly.Api.Services;

public class SwimlaneService
{
    private readonly IMongoCollection<Board> _boardsCollection;
    private readonly IMongoCollection<Card> _cardsCollection;
    private readonly MongoClient _mongoClient;

    public SwimlaneService(MongoDbProvider mongoDbProvider, BoardService boardService)
    {
        _boardsCollection = mongoDbProvider.GetBoardsCollection();
        _cardsCollection = mongoDbProvider.GetCardsCollection();
        _mongoClient = mongoDbProvider.Client;
    }

    public IQueryable<Swimlane> GetSwimlanesByBoardId(ObjectId boardId)
    {
        return _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .SelectMany(b => b.Swimlanes);
    }

    public IQueryable<Swimlane> GetSwimlanesByBoardId(IClientSessionHandle session, ObjectId boardId)
    {
        return _boardsCollection
            .AsQueryable(session)
            .Where(b => b.Id == boardId)
            .SelectMany(b => b.Swimlanes);
    }

    public async Task<DateTime> CreateSwimlaneAsync(ObjectId boardId, ObjectId userId, Swimlane swimlane,
        IClientSessionHandle session, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable(session)
            .Where(x => x.Id == boardId)
            .Select(x => new
            {
                Member = x.Members.FirstOrDefault(m => m.UserId == userId),
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (updatedAt != default && updatedAt != result.UpdatedAt)
            throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
        if (result.Member?.Role != BoardRole.Owner && result.Member?.Role != BoardRole.Admin)
            throw new ForbiddenException("User is not authorized to create a swimlane on this board.");
        var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        if (updatedAt != default)
            filter &= Builders<Board>.Filter.Eq(b => b.UpdatedAt, updatedAt);
        var now = DateTime.UtcNow;
        var update = Builders<Board>.Update
            .Push(b => b.Swimlanes, swimlane)
            .Set(b => b.UpdatedAt, now);
        UpdateResult updateResult = await _boardsCollection.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken);
        if (updatedAt != default && updateResult.MatchedCount == 0)
            throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
        if (updateResult.MatchedCount != 0 && updateResult.ModifiedCount == 0)
            throw new InvalidOperationException("An unexpected error occurred while creating swimlane. Board has not been modified.");
        return now;
    }

    public async Task<(Swimlane, DateTime)> CreateAndFindSwimlaneAsync(ObjectId boardId, ObjectId userId,
        Swimlane swimlane, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ctx) =>
        {
            var newUpdatedAt = await CreateSwimlaneAsync(boardId, userId, swimlane, s, updatedAt, ctx);
            return (await GetSwimlanesByBoardId(s, boardId).SingleAsync(s => s.Id == swimlane.Id, ctx), newUpdatedAt);
        }, cancellationToken: cancellationToken);
    }

    public async Task<DateTime> UpdateSwimlaneAsync(ObjectId boardId, ObjectId userId, Swimlane swimlane,
        IClientSessionHandle session, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable(session)
            .Where(b => b.Id == boardId)
            .Select(b => new
            {
                Member = b.Members.FirstOrDefault(m => m.UserId == userId),
                Swimlane = b.Swimlanes.FirstOrDefault(s => s.Id == swimlane.Id),
                b.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (updatedAt != default && updatedAt != result.UpdatedAt)
            throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
        if (result.Member == null)
            throw new ForbiddenException("User is not a member of this board.");
        if (result.Member.Role != BoardRole.Owner && result.Member.Role != BoardRole.Admin)
            throw new ForbiddenException("User is not authorized to update a swimlane on this board.");
        if (result.Swimlane == null)
            throw new RecordDoesNotExist("Swimlane has not been found.");
        var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        if (updatedAt != default)
            filter &= Builders<Board>.Filter.Eq(b => b.UpdatedAt, updatedAt);
        var now = DateTime.UtcNow;
        var update = Builders<Board>.Update
            .Set("swimlanes.$[sw].title", swimlane.Title)
            .Set("swimlanes.$[sw].color", swimlane.Color)
            .Set(b => b.UpdatedAt, now);
        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("sw._id", swimlane.Id)),
        };
        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
        UpdateResult updateResult = await _boardsCollection.UpdateOneAsync(session, filter, update, updateOptions, cancellationToken);
        if (updatedAt != default && updateResult.MatchedCount == 0)
            throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
        if (updateResult.MatchedCount != 0 && updateResult.ModifiedCount == 0)
            throw new InvalidOperationException("An unexpected error occurred while updating swimlane. Board has not been modified.");
        return now;
    }

    public async Task<(Swimlane, DateTime)> UpdateAndFindSwimlaneAsync(ObjectId boardId, ObjectId userId,
        Swimlane swimlane, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ctx) =>
        {
            var newUpdatedAt = await UpdateSwimlaneAsync(boardId, userId, swimlane, s, updatedAt, ctx);
            return (await GetSwimlanesByBoardId(s, boardId).SingleAsync(s => s.Id == swimlane.Id, ctx), newUpdatedAt);
        }, cancellationToken: cancellationToken);
    }

    public async Task<DateTime> DeleteSwimlaneAsync(ObjectId boardId, ObjectId swimlaneId,
        ObjectId userId, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ctx) =>
        {
            var result = await _boardsCollection
                .AsQueryable(s)
                .Where(b => b.Id == boardId)
                .Select(b => new
                {
                    Swimlane = b.Swimlanes.FirstOrDefault(s => s.Id == swimlaneId),
                    Member = b.Members.FirstOrDefault(m => m.UserId == userId),
                    b.UpdatedAt
                })
                .FirstOrDefaultAsync(ctx)
                ?? throw new RecordDoesNotExist("Board has not been found.");
            if (updatedAt != default && updatedAt != result.UpdatedAt)
                throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
            if (result.Member?.Role != BoardRole.Owner && result.Member?.Role != BoardRole.Admin)
                throw new ForbiddenException("User is not authorized to delete this swimlane.");
            if (result.Swimlane == null)
                throw new RecordDoesNotExist("Swimlane has not been found.");
            var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
            if (updatedAt != default)
                filter &= Builders<Board>.Filter.Eq(b => b.UpdatedAt, updatedAt);
            var now = DateTime.UtcNow;
            var update = Builders<Board>.Update
                .Set(b => b.UpdatedAt, now)
                .PullFilter(b => b.Swimlanes, Builders<Swimlane>.Filter.Eq(s => s.Id, swimlaneId)
            );
            UpdateResult updateResult = await _boardsCollection.UpdateOneAsync(s, filter, update, cancellationToken: ctx);
            if (updatedAt != default && updateResult.MatchedCount == 0)
                throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
            if (updateResult.MatchedCount != 0 && updateResult.ModifiedCount == 0)
                throw new InvalidOperationException("An unexpected error occurred while deleting swimlane. Board has not been modified.");
            await _cardsCollection.DeleteManyAsync(s, x => x.SwimlaneId == swimlaneId, cancellationToken: ctx);
            return now;
        }, cancellationToken: cancellationToken);
    }
}
