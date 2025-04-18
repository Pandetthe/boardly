using Boardly.Backend.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Boardly.Backend.Services;

public class BoardService(MongoDbProvider mongoDbProvider, ILogger<BoardService> logger)
{
    private readonly IMongoCollection<Board> _boardsCollection = mongoDbProvider.GetBoardsCollection();
    private readonly ILogger<BoardService> _logger = logger;
    public async Task AddBoardAsync(Board board, CancellationToken cancellationToken = default)
    {
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
                .Set(b => b.Description, board.Description)
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

    public async Task DeleteBoardAsync(ObjectId id, ObjectId userId, CancellationToken cancellationToken = default)
    {
        try
        {
            //var filterWithAccess = Builders<Board>.Filter.And(
            //    Builders<Board>.Filter.Eq(b => b.Id, id),
            //    Builders<Board>.Filter.ElemMatch(b => b.Members, member => member.UserId == userId)
            //);
            //var deletedBoard = await _boardsCollection.FindOneAndDeleteAsync(filterWithAccess, cancellationToken: cancellationToken);
            
            // TODO
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting a board.");
            throw new InvalidOperationException("An unexpected error occurred while deleting a board.", ex);
        }
    }
}
