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
    private readonly BoardService _boardService;

    public ListService(MongoDbProvider mongoDbProvider, BoardService boardService)
    {
        _boardsCollection = mongoDbProvider.GetBoardsCollection();
        _cardsCollection = mongoDbProvider.GetCardsCollection();
        _boardService = boardService;
    }

    public async Task<List?> GetListByIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .Select(x => new
            {
                Member = x.Members.FirstOrDefault(x => x.UserId == userId),
                List = x.Swimlanes.Where(y => y.Id == swimlaneId).Select(y => y.Lists.FirstOrDefault(l => l.Id == listId)).FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (result.Member == null)
            throw new ForbidenException("User is not a member of this board.");
        return result.List;
    }

    public async Task<IEnumerable<List>> GetListsBySwimlaneIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(x => x.Id == boardId)
            .Select(x => new
            {
                x.Id,
                Member = x.Members.FirstOrDefault(m => m.UserId == userId),
                Lists = x.Swimlanes.Where(y => y.Id == swimlaneId).Select(y => y.Lists).FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (result == null || result.Id == default)
            throw new RecordDoesNotExist("Board has not been found.");
        if (result.Lists == null)
            throw new RecordDoesNotExist("Swimlane has not been found.");
        if (result.Member == null)
            throw new ForbidenException("User is not a member of this board.");
        return result.Lists;
    }

    public async Task CreateListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, List list, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .Select(x => new
            {
                Member = x.Members.FirstOrDefault(m => m.UserId == userId)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (result.Member == null)
            throw new ForbidenException("User is not a member of this board.");
        if (result.Member.Role != BoardRole.Owner && result.Member.Role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to create a swimlane on this board.");

        var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        var update = Builders<Board>.Update
            .Push("swimlanes.$[sw].lists", list)
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

    public async Task UpdateListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, List list, CancellationToken cancellationToken = default)
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
            .Set("swimlanes.$[sw].lists.$[lst].title", list.Title)
            .Set("swimlanes.$[sw].lists.$[lst].color", list.Color)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);
        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("sw._id", swimlaneId)),
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("lst._id", list.Id))
        };
        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
        var updateResult = await _boardsCollection.UpdateOneAsync(filter, update, updateOptions, cancellationToken);
        if (updateResult.ModifiedCount == 0)
            throw new RecordDoesNotExist("List has not been found.");
    }

    public async Task DeleteListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        BoardRole role = await _boardService.CheckUserBoardRoleAsync(boardId, userId, cancellationToken)
            ?? throw new ForbidenException("You are not a member of this board.");
        if (role != BoardRole.Owner && role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to delete this swimlane.");
        var boardFilter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        var update = Builders<Board>.Update.PullFilter(
            "swimlanes.$[sw].lists",
            Builders<BsonDocument>.Filter.Eq("_id", listId)
        );
        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("sw._id", swimlaneId))
        };
        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
        UpdateResult result = await _boardsCollection.UpdateOneAsync(boardFilter, update, updateOptions, cancellationToken);
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("List has not been found.");
        await _cardsCollection.DeleteManyAsync(x => x.ListId == listId, cancellationToken: cancellationToken);
    }
}
