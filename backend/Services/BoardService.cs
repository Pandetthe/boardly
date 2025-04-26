using Boardly.Backend.Entities;
using Boardly.Backend.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Boardly.Backend.Services;

public class BoardService(MongoDbProvider mongoDbProvider, ILogger<BoardService> logger)
{
    private readonly IMongoCollection<Board> _boardsCollection = mongoDbProvider.GetBoardsCollection();
    private readonly ILogger<BoardService> _logger = logger;

    public async Task CreateBoardAsync(Board board, CancellationToken cancellationToken = default)
    {
        if (board.Members.Count(x => x.Role == BoardRole.Owner) != 1)
            throw new ArgumentException("Board must have at least one member that is an owner.");
        try
        {
            await _boardsCollection.InsertOneAsync(board, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while adding a board.");
            throw new InvalidOperationException("An unexpected error occurred while adding a board.", ex);
        }
    }

    public async Task AddMembersAsync(ObjectId boardId, HashSet<Member> members, CancellationToken cancellationToken = default)
    {
        try
        {
            var board = await _boardsCollection.Find(b => b.Id == boardId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Board not found.");

            if (board.Members.Overlaps(members))
                throw new RecordAlreadyExists("Some members already exist in the board.");

            board.Members.UnionWith(members);

            var update = Builders<Board>.Update.Set(b => b.Members, board.Members);
            await _boardsCollection.UpdateOneAsync(b => b.Id == boardId, update, cancellationToken: cancellationToken);
        }
        catch (RecordAlreadyExists) { throw; }
        catch (RecordDoesNotExist) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while adding members.");
            throw new InvalidOperationException("An unexpected error occurred while adding members.", ex);
        }
    }

    public async Task RemoveMembersAsync(ObjectId boardId, HashSet<Member> members, CancellationToken cancellationToken = default)
    {
        try
        {
            var board = await _boardsCollection.Find(b => b.Id == boardId).FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Board not found.");

            var intersection = new HashSet<Member>(board.Members);
            board.Members.IntersectWith(members);
            if (intersection.Count <= 0)
                throw new RecordDoesNotExist("There are not any members to remove.");
            board.Members.ExceptWith(members);
            var filter = Builders<Board>.Filter.Eq(b => b.Id, boardId);
            var update = Builders<Board>.Update.Set(b => b.Members, board.Members);
            await _boardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
        catch (RecordDoesNotExist) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while adding members.");
            throw new InvalidOperationException("An unexpected error occurred while adding members.", ex);
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

    public async Task UpdateBoardWithRoleCheckAsync(Board board, ObjectId userId, CancellationToken cancellationToken = default)
    {
        IEnumerable<Member> owners = board.Members.Where(x => x.Role == BoardRole.Owner);
        if (owners.Count() != 1)
            throw new ArgumentException("Board must have at least one member that is an owner.");
        var existingBoard = await GetBoardByIdAsync(board.Id, cancellationToken) ??
            throw new RecordDoesNotExist("Board has not been found.");
        BoardRole? role = existingBoard.Members.FirstOrDefault(x => x.UserId == userId)?.Role;
        if (role == null || (role != BoardRole.Owner && role != BoardRole.Admin))
            throw new ForbidenException("User is not authorized to update this board.");
        IEnumerable<Member> existingOwners = existingBoard.Members.Where(x => x.Role == BoardRole.Owner);
        if (existingOwners.Count() != 1)
            throw new InvalidOperationException("An unexpected error occurred while updating board. Board has more or less than one owner!");
        if (existingOwners.First() != owners.First() && existingOwners.First().UserId != userId)
            throw new ForbidenException("User is not authorized to change the owner of this board.");
        UpdateResult result;
        try
        {
            board.UpdatedAt = DateTime.UtcNow;
            var filter = Builders<Board>.Filter.Eq(b => b.Id, board.Id);
            var update = Builders<Board>.Update
                .Set(b => b.Title, board.Title)
                .Set(b => b.Members, board.Members)
                .Set(b => b.UpdatedAt, board.UpdatedAt);

            result = await _boardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while updating a board.");
            throw new InvalidOperationException("An unexpected error occurred while updating a board.", ex);
        }

        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("Board has not been found.");
    }

    public async Task<BoardRole?> CheckUserBoardRoleAsync(ObjectId id, ObjectId userId, CancellationToken cancellationToken = default)
    {
        HashSet<Member>? members = null;
        try
        {
            members = await _boardsCollection.Find(b => b.Id == id).Project(x => x.Members).FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while checking user board role.");
            throw new InvalidOperationException("An unexpected error occurred while checking user board role.", ex);
        }
        if (members == null)
            throw new RecordDoesNotExist("Board has not been found.");
        return members.FirstOrDefault(x => x.UserId == userId)?.Role;
    }

    public async Task DeleteBoardWithRoleCheckAsync(ObjectId id, ObjectId userId, CancellationToken cancellationToken = default)
    {
        BoardRole? role = await CheckUserBoardRoleAsync(id, userId, cancellationToken);
        if (role == null || role != BoardRole.Owner)
            throw new ForbidenException("User is not authorized to delete this board.");
        DeleteResult result;
        try
        {
            var filterWithAccess = Builders<Board>.Filter.And(
                Builders<Board>.Filter.Eq(b => b.Id, id)
            );
            result = await _boardsCollection.DeleteOneAsync(filterWithAccess, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while deleting a board.");
            throw new InvalidOperationException("An unexpected error occurred while deleting a board.", ex);
        }
        if (result.DeletedCount == 0)
            throw new RecordDoesNotExist("Board has not been found.");
    }
}
