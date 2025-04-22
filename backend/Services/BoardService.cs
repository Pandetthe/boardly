using Boardly.Backend.Entities;
using Boardly.Backend.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Boardly.Backend.Services;

public class BoardService(MongoDbProvider mongoDbProvider, ILogger<BoardService> logger)
{
    private readonly IMongoCollection<Board> _boardsCollection = mongoDbProvider.GetBoardsCollection();
    private readonly ILogger<BoardService> _logger = logger;
    public async Task AddBoardAsync(Board board, CancellationToken cancellationToken = default)
    {
        if (board.Members.Count == 0 || board.Members.All(x => x.Role != BoardRole.Owner))
            throw new ArgumentException("Board must have at least one member that is an owner.");
        try
        {
            await _boardsCollection.InsertOneAsync(board, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while adding a user.");
            throw new InvalidOperationException("An unexpected error occurred while adding a user.", ex);
        }
    }

    public async Task<Board?> GetBoardByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _boardsCollection.Find(b => b.Id == id, null).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving boards by user ID.");
            throw new InvalidOperationException("An unexpected error occurred while retrieving boards by user ID.", ex);
        }
    }

    public async Task<IEnumerable<Board>> GetBoardsByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var boardsCursor = await _boardsCollection.FindAsync(
                b => b.Members.Any(x => x.UserId == userId),
                cancellationToken: cancellationToken
            );
            return boardsCursor.ToEnumerable(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while retrieving boards by user ID.");
            throw new InvalidOperationException("An unexpected error occurred while retrieving boards by user ID.", ex);
        }
    }

    public async Task UpdateBoardAsync(Board board, CancellationToken cancellationToken = default)
    {
        try
        {
            board.UpdatedAt = DateTime.UtcNow;
            var filter = Builders<Board>.Filter.Eq(b => b.Id, board.Id);
            var update = Builders<Board>.Update
                .Set(b => b.Title, board.Title)
                .Set(b => b.Members, board.Members)
                .Set(b => b.UpdatedAt, board.UpdatedAt);

            await _boardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while updating a board.");
            throw new InvalidOperationException("An unexpected error occurred while updating a board.", ex);
        }
    }

    public async Task<BoardRole?> CheckUserBoardRoleAsync(ObjectId id, ObjectId userId, CancellationToken cancellationToken = default)
    {
        List<Member>? members = null;
        try
        {
            members = await _boardsCollection.Find(b => b.Id == id).Project(x => x.Members).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting a board.");
            throw new InvalidOperationException("An unexpected error occurred while deleting a board.", ex);
        }
        if (members == null)
            throw new RecordDoesNotExist("Board has not been found.");
        return members.FirstOrDefault(x => x.UserId == userId)?.Role;
    }

    public async Task DeleteBoard(ObjectId id, CancellationToken cancellationToken = default)
    {
        Board? deletedBoard = null;
        try
        {
            var filterWithAccess = Builders<Board>.Filter.And(
                Builders<Board>.Filter.Eq(b => b.Id, id)
            );
            deletedBoard = await _boardsCollection.FindOneAndDeleteAsync(filterWithAccess, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting a board.");
            throw new InvalidOperationException("An unexpected error occurred while deleting a board.", ex);
        }
        if (deletedBoard == null)
            throw new RecordDoesNotExist("Board has not beed found.");
    }

    public async Task DeleteBoardWithRoleCheckAsync(ObjectId id, ObjectId userId, CancellationToken cancellationToken = default)
    {
        BoardRole? role = await CheckUserBoardRoleAsync(id, userId, cancellationToken);
        if (role == null || role != BoardRole.Owner)
            throw new UnauthorizedException("User is not authorized to delete this board.");
        await DeleteBoard(id, cancellationToken);
    }
}
