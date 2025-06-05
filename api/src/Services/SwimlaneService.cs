using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;

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
        Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (board.Members.All(x => x.UserId != userId))
            throw new ForbidenException("User is not a member of this board.");
        return board.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId);
    }

    public async Task<IEnumerable<Swimlane>> GetSwimlanesByBoardIdAsync(ObjectId boardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (board.Members.All(x => x.UserId != userId))
            throw new ForbidenException("User is not a member of this board.");
        return board.Swimlanes.AsEnumerable();
    }

    public async Task CreateSwimlaneAsync(ObjectId boardId, ObjectId userId, Swimlane swimlane, CancellationToken cancellationToken = default)
    {
        Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Board has not been found.");
        Member member = board.Members.FirstOrDefault(x => x.UserId == userId)
            ?? throw new ForbidenException("User is not a member of this board.");
        if (member.Role != BoardRole.Owner && member.Role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to create a swimlane on this board.");
        board.Swimlanes.Add(swimlane);
        board.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<Board>.Filter.Eq(b => b.Id, board.Id);
        var update = Builders<Board>.Update
            .Set(b => b.Swimlanes, board.Swimlanes)
            .Set(b => b.UpdatedAt, board.UpdatedAt);
        UpdateResult result = await _boardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("Board has not been found.");
    }

    public async Task UpdateSwimlaneAsync(ObjectId boardId, ObjectId userId, Swimlane swimlane, CancellationToken cancellationToken = default)
    {
        Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Board has not been found.");
        Member member = board.Members.FirstOrDefault(x => x.UserId == userId)
            ?? throw new ForbidenException("User is not a member of this board.");
        if (member.Role != BoardRole.Owner && member.Role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to update a swimlane on this board.");
        Swimlane selectedSwimlane = board.Swimlanes.FirstOrDefault(x => x.Id == swimlane.Id)
            ?? throw new RecordDoesNotExist("Swimlane has not been found.");
        selectedSwimlane.Title = swimlane.Title;
        selectedSwimlane.Color = swimlane.Color;
        board.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<Board>.Filter.Eq(b => b.Id, board.Id);
        var update = Builders<Board>.Update
            .Set(b => b.Swimlanes, board.Swimlanes)
            .Set(b => b.UpdatedAt, board.UpdatedAt);
        UpdateResult result = await _boardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0)
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
