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
    private readonly BoardService _boardService;

    public TagService(MongoDbProvider mongoDbProvider, BoardService boardService)
    {
        _boardsCollection = mongoDbProvider.GetBoardsCollection();
        _cardsCollection = mongoDbProvider.GetCardsCollection();
        _boardService = boardService;
    }

    public async Task<Entities.Board.Tag?> GetTagByIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId tagId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .Select(x => new
            {
                Member = x.Members.FirstOrDefault(m => m.UserId == userId),
                Tag = x.Swimlanes.Where(s => s.Id == swimlaneId)
                    .Select(s => s.Tags.FirstOrDefault(t => t.Id == tagId)).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (result.Member == null)
            throw new ForbidenException("User is not a member of this board.");
        return result.Tag;
    }

    public async Task<IEnumerable<Entities.Board.Tag>> GetTagsBySwimlaneIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .Select(x => new
            {
                Member = x.Members.FirstOrDefault(m => m.UserId == userId),
                Tags = x.Swimlanes.Where(s => s.Id == swimlaneId)
                    .Select(s => s.Tags).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (result.Tags == null)
            throw new RecordDoesNotExist("Swimlane has not been found.");
        if (result.Member == null)
            throw new ForbidenException("User is not a member of this board.");
        return result.Tags;
    }

    public async Task CreateTagAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, Entities.Board.Tag tag, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .Select(x => new
            {
                Member = x.Members.FirstOrDefault(m => m.UserId == userId),
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (result.Member == null)
            throw new ForbidenException("User is not a member of this board.");
        if (result.Member.Role != BoardRole.Owner && result.Member.Role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to create a swimlane on this board.");
        var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        var update = Builders<Board>.Update
            .Push("swimlanes.$[sw].tags", tag)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);

        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("sw._id", swimlaneId))
        };
        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };

        var updateResult = await _boardsCollection.UpdateOneAsync(filter, update, updateOptions, cancellationToken);

        if (updateResult.ModifiedCount == 0)
            throw new RecordDoesNotExist("Swimlane has not been found.");
    }

    public async Task UpdateTagAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, Entities.Board.Tag tag, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .Select(x => new
            {
                Member = x.Members.FirstOrDefault(x => x.UserId == userId),
                Swimlane = x.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId)
            }).FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (result.Member == null)
            throw new ForbidenException("User is not a member of this board.");
        if (result.Member.Role != BoardRole.Owner && result.Member.Role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to update a swimlane on this board.");
        if (result.Swimlane == null)
            throw new RecordDoesNotExist("Swimlane has not been found.");
        var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        var update = Builders<Board>.Update
            .Set("swimlanes.$[sw].tags.$[tg].title", tag.Title)
            .Set("swimlanes.$[sw].tags.$[tg].color", tag.Color)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);
        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("sw._id", swimlaneId)),
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("tg._id", tag.Id))
        };
        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
        var updateResult = await _boardsCollection.UpdateOneAsync(filter, update, updateOptions, cancellationToken);
        if (updateResult.ModifiedCount == 0)
            throw new RecordDoesNotExist("Tag has not been found.");
    }

    public async Task DeleteTagAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId tagId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        BoardRole role = await _boardService.CheckUserBoardRoleAsync(boardId, userId, cancellationToken)
            ?? throw new ForbidenException("User is not a member of this board.");
        if (role != BoardRole.Owner && role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to delete this swimlane.");
        var boardFilter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        var update = Builders<Board>.Update.PullFilter(
            "swimlanes.$[sw].tags",
            Builders<BsonDocument>.Filter.Eq("_id", tagId)
        );
        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("sw._id", swimlaneId))
        };
        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
        var result = await _boardsCollection.UpdateOneAsync(boardFilter, update, updateOptions, cancellationToken);
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("Tag has not been found.");

        var cardFilter = Builders<Card>.Filter.AnyEq(c => c.Tags, tagId);
        var cardUpdate = Builders<Card>.Update.Pull(c => c.Tags, tagId);
        await _cardsCollection.UpdateManyAsync(cardFilter, cardUpdate, cancellationToken: cancellationToken);
    }
}
