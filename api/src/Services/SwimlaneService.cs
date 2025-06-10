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
    private readonly BoardService _boardService;

    public SwimlaneService(MongoDbProvider mongoDbProvider, BoardService boardService)
    {
        _boardsCollection = mongoDbProvider.GetBoardsCollection();
        _cardsCollection = mongoDbProvider.GetCardsCollection();
        _boardService = boardService;
    }

    public async Task<Swimlane?> GetSwimlaneByIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(x => x.Id == boardId)
            .Select(x => new
            {
                Member = x.Members.FirstOrDefault(m => m.UserId == userId),
                Swimlane = x.Swimlanes.FirstOrDefault(s => s.Id == swimlaneId)
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (result.Member == null)
            throw new ForbidenException("User is not a member of this board.");
        return result.Swimlane;
    }

    public async Task<IEnumerable<Swimlane>> GetSwimlanesByBoardIdAsync(ObjectId boardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(x => x.Id == boardId)
            .Select(x => new
            {
                Member = x.Members.FirstOrDefault(m => m.UserId == userId),
                x.Swimlanes
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (result.Member == null)
            throw new ForbidenException("User is not a member of this board.");
        return result.Swimlanes;
    }

    public async Task CreateSwimlaneAsync(ObjectId boardId, ObjectId userId, Swimlane swimlane, CancellationToken cancellationToken = default)
    {
        BoardRole? role = await _boardService.CheckUserBoardRoleAsync(boardId, userId, cancellationToken)
            ?? throw new ForbidenException("User is not a member of this board.");
        if (role != BoardRole.Owner && role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to create a swimlane on this board.");
        var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        var update = Builders<Board>.Update
            .Push(b => b.Swimlanes, swimlane)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);
        UpdateResult result = await _boardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("Board has not been found.");
    }

    public async Task UpdateSwimlaneAsync(ObjectId boardId, ObjectId userId, Swimlane swimlane, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(x => x.Id == boardId)
            .Select(x => new
            {
                Member = x.Members.FirstOrDefault(m => m.UserId == userId),
                Swimlane = x.Swimlanes.FirstOrDefault(s => s.Id == swimlane.Id)
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (result.Member == null)
            throw new ForbidenException("User is not a member of this board.");
        if (result.Member.Role != BoardRole.Owner && result.Member.Role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to update a swimlane on this board.");
        if (result.Swimlane == null)
            throw new RecordDoesNotExist("Swimlane has not been found.");
        var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        var update = Builders<Board>.Update
            .Set("swimlanes.$[sw].title", swimlane.Title)
            .Set("swimlanes.$[sw].color", swimlane.Color)
            .Set(b => b.UpdatedAt, DateTime.UtcNow);
        var arrayFilters = new List<ArrayFilterDefinition>
        {
            new BsonDocumentArrayFilterDefinition<BsonDocument>(new BsonDocument("sw._id", swimlane.Id)),
        };
        var updateOptions = new UpdateOptions { ArrayFilters = arrayFilters };
        UpdateResult updateResult = await _boardsCollection.UpdateOneAsync(filter, update, updateOptions, cancellationToken);
        if (updateResult.ModifiedCount == 0)
            throw new RecordDoesNotExist("Board has not been found.");
    }

    public async Task DeleteSwimlaneAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        BoardRole role = await _boardService.CheckUserBoardRoleAsync(boardId, userId, cancellationToken)
            ?? throw new ForbidenException("You are not a member of this board.");
        if (role != BoardRole.Owner && role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to delete this swimlane.");
        var update = Builders<Board>.Update.PullFilter(
            b => b.Swimlanes,
            Builders<Swimlane>.Filter.Eq(s => s.Id, swimlaneId)
        );
        UpdateResult result = await _boardsCollection.UpdateOneAsync(x => x.Id == boardId, update, cancellationToken: cancellationToken);
        await _cardsCollection.DeleteManyAsync(x => x.SwimlaneId == swimlaneId, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("Swimlane has not been found.");
    }
}
