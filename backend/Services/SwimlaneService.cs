using Boardly.Backend.Entities;
using Boardly.Backend.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Boardly.Backend.Services;

public class SwimlaneService(MongoDbProvider mongoDbProvider, ILogger<BoardService> logger, BoardService boardService)
{
    private readonly IMongoCollection<Board> _boardsCollection = mongoDbProvider.GetBoardsCollection();
    private readonly ILogger<BoardService> _logger = logger;
    private readonly BoardService _boardService = boardService;

    public async Task<Swimlane?> GetSwimlaneByIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Board has not been found.");
            if (board.Members.All(x => x.UserId != userId))
                throw new ForbidenException("You are not a member of this board.");
            return board.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId);
        }
        catch (ForbidenException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving swimlane by id.");
            throw new InvalidOperationException("An unexpected error occurred while retrieving swimlane by id.", ex);
        }
    }

    public async Task<IEnumerable<Swimlane>> GetSwimlanesByBoardIdAsync(ObjectId boardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            Board board = await _boardsCollection.Find(x => x.Id == boardId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Board has not been found.");
            if (board.Members.All(x => x.UserId != userId))
                throw new ForbidenException("You are not a member of this board.");
            return board.Swimlanes.AsEnumerable();
        }
        catch (ForbidenException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving swimlanes by board id.");
            throw new InvalidOperationException("An unexpected error occurred while retrieving swimlanes by board id.", ex);
        }
    }

    public async Task DeleteSwimlaneAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        BoardRole role = await _boardService.CheckUserBoardRoleAsync(boardId, userId, cancellationToken)
            ?? throw new ForbidenException("You are not a member of this board.");
        if (role != BoardRole.Owner && role != BoardRole.Admin)
            throw new ForbidenException("You are not authorized to delete this swimlane.");
        UpdateResult result;
        try
        {
            var boardFilter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
            var update = Builders<Board>.Update.PullFilter(
                b => b.Swimlanes,
                Builders<Swimlane>.Filter.Eq(s => s.Id, swimlaneId)
            );
            result = await _boardsCollection.UpdateOneAsync(boardFilter, update, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting a board.");
            throw new InvalidOperationException("An unexpected error occurred while deleting a board.", ex);
        }
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("Swimlane has not been found.");
    }
}
