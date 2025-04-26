using Boardly.Backend.Entities;
using Boardly.Backend.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Boardly.Backend.Services;

public class ListService(MongoDbProvider mongoDbProvider, ILogger<BoardService> logger, BoardService boardService)
{
    private readonly IMongoCollection<Board> _boardsCollection = mongoDbProvider.GetBoardsCollection();
    private readonly ILogger<BoardService> _logger = logger;
    private readonly BoardService _boardService = boardService;

    public async Task<List?> GetListByIdAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, CancellationToken cancellationToken = default)
    {
        try
        {
            var board = await _boardsCollection.Find(b => b.Id == boardId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Board not found.");
            var swimlane = board.Swimlanes.FirstOrDefault(x => x.Id == swimlaneId)
                ?? throw new RecordDoesNotExist("Swimlane not found.");
            var list = swimlane.Lists.FirstOrDefault(x => x.Id == listId)
                ?? throw new RecordDoesNotExist("List not found.");
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving a list.");
            throw new InvalidOperationException("An unexpected error occurred while retrieving a list.", ex);
        }
    }

    public async Task CreateListAsync(List list, CancellationToken cancellationToken = default)
    {

    }

    public async Task DeleteListAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, CancellationToken cancellationToken = default)
    {
        UpdateResult result;
        try
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting a list.");
            throw new InvalidOperationException("An unexpected error occurred while deleting a list.", ex);
        }
        //if (result.ModifiedCount == 0)
        //    throw new RecordDoesNotExist("List has not been found.");
    }

    public async Task DeleteListWithRoleCheckAsync(ObjectId boardId, ObjectId swimlaneId, ObjectId listId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        BoardRole? role = await _boardService.CheckUserBoardRoleAsync(boardId, userId, cancellationToken);
        if (role == null || (role != BoardRole.Owner && role != BoardRole.Admin))
            throw new ForbidenException("User is not authorized to delete this list.");
        await DeleteListAsync(boardId, swimlaneId, listId, cancellationToken);
    }
}
