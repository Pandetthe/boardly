using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;

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
        Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (board.Members.All(x => x.UserId != userId))
            throw new ForbidenException("User is not a member of this board.");
        return board.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId)?.Lists.FirstOrDefault(x => x.Id == listId);
    }

    public async Task<IEnumerable<List>> GetListsBySwimlaneIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (board.Members.All(x => x.UserId != userId))
            throw new ForbidenException("User is not a member of this board.");
        return board.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId)?.Lists.AsEnumerable() ?? [];
    }

    public async Task CreateListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, List list, CancellationToken cancellationToken = default)
    {
        Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Board has not been found.");
        Member member = board.Members.FirstOrDefault(x => x.UserId == userId)
            ?? throw new ForbidenException("User is not a member of this board.");
        if (member.Role != BoardRole.Owner && member.Role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to create a swimlane on this board.");
        Swimlane swimlane = board.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId)
            ?? throw new RecordDoesNotExist("Swimlane has not been found.");
        swimlane.Lists.Add(list);
        board.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<Board>.Filter.Eq(b => b.Id, board.Id);
        var update = Builders<Board>.Update
            .Set(b => b.Swimlanes, board.Swimlanes)
            .Set(b => b.UpdatedAt, board.UpdatedAt);
        UpdateResult result = await _boardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("Board has not been found.");
    }

    public async Task UpdateListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, List list, CancellationToken cancellationToken = default)
    {
        Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Board has not been found.");
        Member member = board.Members.FirstOrDefault(x => x.UserId == userId)
            ?? throw new ForbidenException("User is not a member of this board.");
        if (member.Role != BoardRole.Owner && member.Role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to update a swimlane on this board.");
        Swimlane selectedSwimlane = board.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId)
            ?? throw new RecordDoesNotExist("Swimlane has not been found.");
        List selectedList = selectedSwimlane.Lists.FirstOrDefault(x => x.Id == list.Id)
            ?? throw new RecordDoesNotExist("List has not been found.");
        selectedList.Title = list.Title;
        selectedList.Color = list.Color;
        board.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<Board>.Filter.Eq(b => b.Id, board.Id);
        var update = Builders<Board>.Update
            .Set(b => b.Swimlanes, board.Swimlanes)
            .Set(b => b.UpdatedAt, board.UpdatedAt);
        UpdateResult result = await _boardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("Board has not been found.");
    }

    public async Task DeleteListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        BoardRole role = await _boardService.CheckUserBoardRoleAsync(boardId, userId, cancellationToken)
            ?? throw new ForbidenException("You are not a member of this board.");
        if (role != BoardRole.Owner && role != BoardRole.Admin)
        throw new ForbidenException("User is not authorized to delete this swimlane.");
        var boardFilter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        var update = Builders<Board>.Update.PullFilter(
            "swimlanes.$[swimlane].lists",
            Builders<BsonDocument>.Filter.Eq("_id", listId)
        );
        var updateOptions = new UpdateOptions
        {
            ArrayFilters =
            [
                new BsonDocumentArrayFilterDefinition<BsonDocument>(
                    new BsonDocument("swimlane._id", swimlaneId))
            ]
        };
        UpdateResult result = await _boardsCollection.UpdateOneAsync(boardFilter, update, updateOptions, cancellationToken);
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("List has not been found.");
        await _cardsCollection.DeleteManyAsync(x => x.ListId == listId, cancellationToken: cancellationToken);
    }
}
