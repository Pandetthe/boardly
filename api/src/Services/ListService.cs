using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Boardly.Api.Services;

public class ListService
{
    private readonly IMongoCollection<Board> _boardsCollection;
    private readonly IMongoCollection<Card> _cardsCollection;
    private readonly MongoClient _mongoClient;

    public ListService(MongoDbProvider mongoDbProvider)
    {
        _boardsCollection = mongoDbProvider.GetBoardsCollection();
        _cardsCollection = mongoDbProvider.GetCardsCollection();
        _mongoClient = mongoDbProvider.Client;
    }

    public IQueryable<List> GetListBySwimlaneId(ObjectId boardId, ObjectId swimlaneId)
    {
        return _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .SelectMany(b => b.Swimlanes.Where(s => s.Id == swimlaneId).SelectMany(s => s.Lists));
    }

    public IQueryable<List> GetListBySwimlaneId(IClientSessionHandle session, ObjectId boardId, ObjectId swimlaneId)
    {
        return _boardsCollection
            .AsQueryable(session)
            .Where(b => b.Id == boardId)
            .SelectMany(b => b.Swimlanes.Where(s => s.Id == swimlaneId).SelectMany(s => s.Lists));
    }

    public async Task<DateTime> CreateListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, List list,
        IClientSessionHandle session, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable(session)
            .Where(b => b.Id == boardId)
            .Select(b => new
            {
                Member = b.Members.FirstOrDefault(m => m.UserId == userId),
                b.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (updatedAt != default && updatedAt != result.UpdatedAt)
            throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
        if (result.Member?.Role != BoardRole.Owner && result.Member?.Role != BoardRole.Admin)
            throw new ForbiddenException("User is not authorized to create a list on this board.");

        var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        if (updatedAt != default)
            filter &= Builders<Board>.Filter.Eq(b => b.UpdatedAt, updatedAt);
        var now = DateTime.UtcNow;
        var update = Builders<Board>.Update
            .Push("swimlanes.$[sw].lists", list)
            .Set(b => b.UpdatedAt, now);

        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("sw._id", swimlaneId))
        };
        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };

        var updateResult = await _boardsCollection.UpdateOneAsync(session, filter, update, updateOptions, cancellationToken);

        if (updatedAt != default && updateResult.MatchedCount == 0)
            throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
        if (updateResult.MatchedCount != 0 && updateResult.ModifiedCount == 0)
            throw new InvalidOperationException("An unexpected error occurred while creating list. Board has not been modified.");
        return now;
    }

    public async Task<(List, DateTime)> CreateAndFindListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId,
    List list, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ctx) =>
        {
            var newUpdatedAt = await CreateListAsync(boardId, swimlaneId, userId, list, s, updatedAt, ctx);
            return (await GetListBySwimlaneId(s, boardId, swimlaneId).SingleAsync(s => s.Id == list.Id, ctx), newUpdatedAt);
        }, cancellationToken: cancellationToken);
    }

    public async Task<DateTime> UpdateListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, List list,
        IClientSessionHandle session, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable(session)
            .Where(b => b.Id == boardId)
            .Select(b => new
            {
                Member = b.Members.FirstOrDefault(m => m.UserId == userId),
                List = b.Swimlanes.Where(s => s.Id == swimlaneId).Select(s => s.Lists.FirstOrDefault(l => l.Id == list.Id)).FirstOrDefault(),
                b.UpdatedAt
            }).FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (updatedAt != default && updatedAt != result.UpdatedAt)
            throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
        if (result.Member?.Role != BoardRole.Owner && result.Member?.Role != BoardRole.Admin)
            throw new ForbiddenException("User is not authorized to update a list on this board.");
        if (result.List == null)
            throw new RecordDoesNotExist("List has not been found.");
        var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        if (updatedAt != default)
            filter &= Builders<Board>.Filter.Eq(b => b.UpdatedAt, updatedAt);
        var now = DateTime.UtcNow;
        var update = Builders<Board>.Update
            .Set("swimlanes.$[sw].lists.$[lst].title", list.Title)
            .Set("swimlanes.$[sw].lists.$[lst].color", list.Color)
            .Set("swimlanes.$[sw].lists.$[lst].maxWIP", list.MaxWIP)
            .Set(b => b.UpdatedAt, now);
        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("sw._id", swimlaneId)),
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("lst._id", list.Id))
        };
        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
        var updateResult = await _boardsCollection.UpdateOneAsync(session, filter, update, updateOptions, cancellationToken);
        if (updatedAt != default && updateResult.MatchedCount == 0)
            throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
        if (updateResult.MatchedCount != 0 && updateResult.ModifiedCount == 0)
            throw new InvalidOperationException("An unexpected error occurred while updating list. Board has not been modified.");
        return now;
    }

    public async Task<(List, DateTime)> UpdateAndFindListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId,
        List list, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ctx) =>
        {
            var newUpdatedAt = await UpdateListAsync(boardId, swimlaneId, userId, list, s, updatedAt, ctx);
            return (await GetListBySwimlaneId(s, boardId, swimlaneId).SingleAsync(s => s.Id == list.Id, ctx), newUpdatedAt);
        }, cancellationToken: cancellationToken);
    }

    public async Task<DateTime> DeleteListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, ObjectId userId,
        DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ctx) =>
        {
            var result = await _boardsCollection
                .AsQueryable(s)
                .Where(b => b.Id == boardId)
                .Select(b => new
                {
                    List = b.Swimlanes.Where(s => s.Id == swimlaneId).Select(s => s.Lists.FirstOrDefault(l => l.Id == listId)).FirstOrDefault(),
                    Member = b.Members.FirstOrDefault(m => m.UserId == userId),
                    b.UpdatedAt
                })
                .FirstOrDefaultAsync(ctx)
                ?? throw new RecordDoesNotExist("Board has not been found.");
            if (updatedAt != default && updatedAt != result.UpdatedAt)
                throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
            if (result.Member?.Role != BoardRole.Owner && result.Member?.Role != BoardRole.Admin)
                throw new ForbiddenException("User is not authorized to delete this list.");
            if (result.List == null)
                throw new RecordDoesNotExist("List has not been found.");
            var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
            if (updatedAt != default)
                filter &= Builders<Board>.Filter.Eq(b => b.UpdatedAt, updatedAt);
            var now = DateTime.UtcNow;
            var update = Builders<Board>.Update.PullFilter(
                "swimlanes.$[sw].lists",
                Builders<BsonDocument>.Filter.Eq("_id", listId))
                .Set(b => b.UpdatedAt, now);
            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("sw._id", swimlaneId))
            };
            var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
            UpdateResult updateResult = await _boardsCollection.UpdateOneAsync(s, filter, update, updateOptions, ctx);
            if (updatedAt != default && updateResult.MatchedCount == 0)
                throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
            if (updateResult.MatchedCount != 0 && updateResult.ModifiedCount == 0)
                throw new InvalidOperationException("An unexpected error occurred while deleting list. Board has not been modified.");
            await _cardsCollection.DeleteManyAsync(s, x => x.ListId == listId, cancellationToken: ctx);
            return now;
        }, cancellationToken: cancellationToken);
    }
}
