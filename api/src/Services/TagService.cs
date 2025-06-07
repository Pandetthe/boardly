using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;

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
        Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (board.Members.All(x => x.UserId != userId))
            throw new ForbidenException("User is not a member of this board.");
        return board.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId)?.Tags.FirstOrDefault(x => x.Id == tagId);
    }

    public async Task<IEnumerable<Entities.Board.Tag>> GetTagsBySwimlaneIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (board.Members.All(x => x.UserId != userId))
            throw new ForbidenException("User is not a member of this board.");
        return board.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId)?.Tags.AsEnumerable() ?? [];
    }

    public async Task CreateTagAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, Entities.Board.Tag tag, CancellationToken cancellationToken = default)
    {
        Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Board has not been found.");
        Member member = board.Members.FirstOrDefault(x => x.UserId == userId)
            ?? throw new ForbidenException("User is not a member of this board.");
        if (member.Role != BoardRole.Owner && member.Role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to create a swimlane on this board.");
        Swimlane swimlane = board.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId)
            ?? throw new RecordDoesNotExist("Swimlane has not been found.");
        swimlane.Tags.Add(tag);
        board.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<Board>.Filter.Eq(b => b.Id, board.Id);
        var update = Builders<Board>.Update
            .Set(b => b.Swimlanes, board.Swimlanes)
            .Set(b => b.UpdatedAt, board.UpdatedAt);
        UpdateResult result = await _boardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("Board has not been found.");
    }

    public async Task UpdateTagAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, Entities.Board.Tag tag, CancellationToken cancellationToken = default)
    {
        Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Board has not been found.");
        Member member = board.Members.FirstOrDefault(x => x.UserId == userId)
            ?? throw new ForbidenException("User is not a member of this board.");
        if (member.Role != BoardRole.Owner && member.Role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to update a swimlane on this board.");
        Swimlane selectedSwimlane = board.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId)
            ?? throw new RecordDoesNotExist("Swimlane has not been found.");
        Entities.Board.Tag selectedTag = selectedSwimlane.Tags.FirstOrDefault(x => x.Id == tag.Id)
            ?? throw new RecordDoesNotExist("Tag has not been found.");
        selectedTag.Title = tag.Title;
        selectedTag.Color = tag.Color;
        board.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<Board>.Filter.Eq(b => b.Id, board.Id);
        var update = Builders<Board>.Update
            .Set(b => b.Swimlanes, board.Swimlanes)
            .Set(b => b.UpdatedAt, board.UpdatedAt);
        UpdateResult result = await _boardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("Board has not been found.");
    }

    public async Task DeleteTagAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId tagId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        BoardRole role = await _boardService.CheckUserBoardRoleAsync(boardId, userId, cancellationToken)
            ?? throw new ForbidenException("You are not a member of this board.");
        if (role != BoardRole.Owner && role != BoardRole.Admin)
            throw new ForbidenException("User is not authorized to delete this swimlane.");
        var boardFilter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
        var update = Builders<Board>.Update.PullFilter(
            "Swimlanes.$[swimlane].Tags",
            Builders<Entities.Board.Tag>.Filter.Eq(l => l.Id, tagId)
        );
        var updateOptions = new UpdateOptions
        {
            ArrayFilters = [new JsonArrayFilterDefinition<Swimlane>("{ 'swimlane._id': '" + swimlaneId + "' }")]
        };
        UpdateResult result = await _boardsCollection.UpdateOneAsync(boardFilter, update, updateOptions, cancellationToken);
        var cardFilter = Builders<Card>.Filter.AnyEq(c => c.Tags, tagId);
        var cardUpdate = Builders<Card>.Update.Pull(c => c.Tags, tagId);
        await _cardsCollection.UpdateManyAsync(cardFilter, cardUpdate, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("Tag has not been found.");
    }
}
