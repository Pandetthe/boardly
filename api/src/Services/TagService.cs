using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Boardly.Api.Services;

public class TagService
{
    private readonly IMongoCollection<Board> _boardsCollection;
    private readonly IMongoCollection<Card> _cardsCollection;
    private readonly MongoClient _mongoClient;

    public TagService(MongoDbProvider mongoDbProvider)
    {
        _boardsCollection = mongoDbProvider.GetBoardsCollection();
        _cardsCollection = mongoDbProvider.GetCardsCollection();
        _mongoClient = mongoDbProvider.Client;
    }

    public IQueryable<Entities.Board.Tag> GetTagsBySwimlaneIdAsync(ObjectId boardId, ObjectId swimlaneId, IClientSessionHandle? session = null)
    {
        return (session == null
            ? _boardsCollection.AsQueryable()
            : _boardsCollection.AsQueryable(session))
            .Where(b => b.Id == boardId)
            .SelectMany(b => b.Swimlanes
                .Where(s => s.Id == swimlaneId)
                .SelectMany(s => s.Tags));
    }

    public async Task<DateTime> CreateTagAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, Entities.Board.Tag tag,
        IClientSessionHandle session, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable(session)
            .Where(b => b.Id == boardId)
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
            throw new ForbiddenException("User is not authorized to create a tag on this board.");
        var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        if (updatedAt != default)
            filter &= Builders<Board>.Filter.Eq(b => b.UpdatedAt, updatedAt);
        var now = DateTime.UtcNow;
        var update = Builders<Board>.Update
            .Push("swimlanes.$[sw].tags", tag)
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
            throw new InvalidOperationException("An unexpected error occurred while creating tag. Board has not been modified.");
        return now;
    }

    public async Task<(Entities.Board.Tag, DateTime)> CreateAndFindTagAsync(ObjectId boardId, ObjectId swimlaneId, 
        ObjectId userId, Entities.Board.Tag tag, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ctx) =>
        {
            var newUpdatedAt = await CreateTagAsync(boardId, swimlaneId, userId, tag, s, updatedAt, ctx);
            return (await GetTagsBySwimlaneIdAsync(boardId, swimlaneId, s).SingleAsync(t => t.Id == tag.Id, ctx), newUpdatedAt);
        }, cancellationToken: cancellationToken);
    }

    public async Task<DateTime> UpdateTagAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, Entities.Board.Tag tag,
        IClientSessionHandle session, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable(session)
            .Where(b => b.Id == boardId)
            .Select(x => new
            {
                Member = x.Members.FirstOrDefault(x => x.UserId == userId),
                Swimlane = x.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId),
                x.UpdatedAt
            }).FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (updatedAt != default && updatedAt != result.UpdatedAt)
            throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
        if (result.Member?.Role != BoardRole.Owner && result.Member?.Role != BoardRole.Admin)
            throw new ForbiddenException("User is not authorized to update a tag on this board.");
        if (result.Swimlane == null)
            throw new RecordDoesNotExist("Swimlane has not been found.");
        var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        if (updatedAt != default)
            filter &= Builders<Board>.Filter.Eq(b => b.UpdatedAt, updatedAt);
        var now = DateTime.UtcNow;
        var update = Builders<Board>.Update
            .Set("swimlanes.$[sw].tags.$[tg].title", tag.Title)
            .Set("swimlanes.$[sw].tags.$[tg].color", tag.Color)
            .Set(b => b.UpdatedAt, now);
        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("sw._id", swimlaneId)),
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("tg._id", tag.Id))
        };
        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
        var updateResult = await _boardsCollection.UpdateOneAsync(session, filter, update, updateOptions, cancellationToken);
        if (updatedAt != default && updateResult.MatchedCount == 0)
            throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
        if (updateResult.MatchedCount != 0 && updateResult.ModifiedCount == 0)
            throw new InvalidOperationException("An unexpected error occurred while updating tag. Board has not been modified.");
        return now;
    }

    public async Task<(Entities.Board.Tag, DateTime)> UpdateAndFindTagAsync(ObjectId boardId, ObjectId swimlaneId,
        ObjectId userId, Entities.Board.Tag tag, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ctx) =>
        {
            var newUpdatedAt = await UpdateTagAsync(boardId, swimlaneId, userId, tag, s, updatedAt, ctx);
            return (await GetTagsBySwimlaneIdAsync(boardId, swimlaneId, s).SingleAsync(t => t.Id == tag.Id, ctx), newUpdatedAt);
        }, cancellationToken: cancellationToken);
    }

    public async Task<DateTime> DeleteTagAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId tagId, ObjectId userId,
        DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ctx) => 
        {
            var result = await _boardsCollection
                .AsQueryable(s)
                .Where(x => x.Id == boardId)
                .Select(x => new
                {
                    Tag = x.Swimlanes.Where(s => s.Id == swimlaneId).Select(s => s.Tags.FirstOrDefault(t => t.Id == tagId)).FirstOrDefault(),
                    Member = x.Members.FirstOrDefault(m => m.UserId == userId),
                    x.UpdatedAt
                })
                .FirstOrDefaultAsync(ctx)
                ?? throw new RecordDoesNotExist("Board has not been found.");
            if (updatedAt != default && updatedAt != result.UpdatedAt)
                throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
            if (result.Member?.Role != BoardRole.Owner && result.Member?.Role != BoardRole.Admin)
                throw new ForbiddenException("User is not authorized to delete this tag.");
            if (result.Tag == null)
                throw new RecordDoesNotExist("Tag has not been found.");
            var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
            var now = DateTime.UtcNow;
            var update = Builders<Board>.Update.PullFilter(
                "swimlanes.$[sw].tags",
                Builders<BsonDocument>.Filter.Eq("_id", tagId))
                .Set(b => b.UpdatedAt, now);
            var arrayFilters = new List<ArrayFilterDefinition>
            {
                new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("sw._id", swimlaneId))
            };
            var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
            var updateResult = await _boardsCollection.UpdateOneAsync(s, filter, update, updateOptions, ctx);
            if (updatedAt != default && updateResult.MatchedCount == 0)
                throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
            if (updateResult.MatchedCount != 0 && updateResult.ModifiedCount == 0)
                throw new InvalidOperationException("An unexpected error occurred while deleting tag. Board has not been modified.");

            var cardFilter = Builders<Card>.Filter.AnyEq(c => c.Tags, tagId);
            var cardUpdate = Builders<Card>.Update.Pull(c => c.Tags, tagId);
            await _cardsCollection.UpdateManyAsync(s, cardFilter, cardUpdate, cancellationToken: ctx);
            return now;
        }, cancellationToken: cancellationToken);
    }
}
