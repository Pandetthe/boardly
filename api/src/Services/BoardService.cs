using Boardly.Api.Entities;
using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using Boardly.Api.Models.Dtos;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Boardly.Api.Services;

public class BoardService
{
    private readonly IMongoCollection<Board> _boardsCollection;
    private readonly IMongoCollection<Card> _cardsCollection;
    private readonly IMongoCollection<User> _usersCollection;
    private readonly ILogger<BoardService> _logger;

    public BoardService(MongoDbProvider mongoDbProvider, ILogger<BoardService> logger)
    {
        _boardsCollection = mongoDbProvider.GetBoardsCollection();
        _cardsCollection = mongoDbProvider.GetCardsCollection();
        _usersCollection = mongoDbProvider.GetUsersCollection();
        _logger = logger;
    }

    public async Task CreateBoardAsync(Board board, CancellationToken cancellationToken = default)
    {
        if (board.Members.Count(x => x.Role == BoardRole.Owner) != 1)
            throw new ArgumentException("Board must have at least one member that is an owner.");
        await _boardsCollection.InsertOneAsync(board, cancellationToken: cancellationToken);
    }

    public async Task<Board?> GetRawBoardByIdAsync(ObjectId id, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var board = await _boardsCollection
            .AsQueryable()
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        if (board == null)
            return null;
        if (!board.Members.Any(x => x.UserId == userId))
            throw new ForbidenException("User is not a member of this board.");
        return board;
    }

    public async Task<BoardWithUser?> GetBoardByIdAsync(ObjectId id, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(x => x.Id == id)
            .SelectMany(x => x.Members.Select(member => new { Board = x, Member = member }))
            .Join(
                _usersCollection,
                x => x.Member.UserId,
                user => user.Id,
                (x, user) => new
                {
                    x.Board,
                    Member = new MemberWithUser
                    {
                        UserId = x.Member.UserId,
                        Role = x.Member.Role,
                        IsActive = x.Member.IsActive,
                        Nickname = user.Nickname
                    }
                }
            )
            .GroupBy(x => x.Board.Id)
            .Select(x => new
            {
                x.First().Board,
                Members = x.Select(m => m.Member),
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (!result.Members.Any(x => x.UserId == userId))
            throw new ForbidenException("User is not a member of this board.");
        return new BoardWithUser
        {
            Id = result.Board.Id,
            Title = result.Board.Title,
            Members = [.. result.Members],
            Swimlanes = [.. result.Board.Swimlanes],
            CreatedAt = result.Board.CreatedAt.ToUniversalTime(),
            UpdatedAt = result.Board.UpdatedAt.ToUniversalTime()
        };
    }

    public async Task ChangeUserActivity(ObjectId id, ObjectId userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Board>.Filter.And(
            Builders<Board>.Filter.Eq("_id", id),
            Builders<Board>.Filter.ElemMatch("members", Builders<Member>.Filter.Eq("userId", userId))
        );

        var update = Builders<Board>.Update.Set("members.$.isActive", isActive);

        await _boardsCollection.UpdateOneAsync(filter, update, null, cancellationToken);
    }

    public async Task<IEnumerable<BoardWithUser>> GetBoardsByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(x => x.Members.Any(y => y.UserId == userId))
            .SelectMany(x => x.Members.Select(member => new { Board = x, Member = member }))
            .Join(
                _usersCollection,
                x => x.Member.UserId,
                user => user.Id,
                (x, user) => new
                {
                    x.Board,
                    Member = new MemberWithUser
                    {
                        UserId = x.Member.UserId,
                        Role = x.Member.Role,
                        IsActive = x.Member.IsActive,
                        Nickname = user.Nickname
                    }
                }
            )
            .GroupBy(x => x.Board.Id)
            .Select(x => new
            {
                x.First().Board,
                Members = x.Select(m => m.Member),
            })
            .ToListAsync(cancellationToken);
        return result.Select(b => new BoardWithUser
        {
            Id = b.Board.Id,
            Title = b.Board.Title,
            Members = [.. b.Members],
            Swimlanes = [.. b.Board.Swimlanes],
            CreatedAt = b.Board.CreatedAt.ToUniversalTime(),
            UpdatedAt = b.Board.UpdatedAt.ToUniversalTime()
        });
    }

    public async Task<List<Board>> GetRawBoardsByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        return await _boardsCollection
            .AsQueryable()
            .Where(b => b.Members.Any(m => m.UserId == userId))
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateBoardAsync(Board board, ObjectId userId, CancellationToken cancellationToken = default)
    {
        IEnumerable<Member> owners = board.Members.Where(x => x.Role == BoardRole.Owner);
        if (owners.Count() != 1)
            throw new ArgumentException("Board must have at least one member that is an owner.");
        var members = await _boardsCollection
            .AsQueryable()
            .Where(x => x.Id == board.Id)
            .Select(x => x.Members)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        BoardRole? role = members.FirstOrDefault(x => x.UserId == userId)?.Role;
        if (role == null || (role != BoardRole.Owner && role != BoardRole.Admin))
            throw new ForbidenException("User is not authorized to update this board.");
        IEnumerable<Member> existingOwners = members.Where(x => x.Role == BoardRole.Owner);
        if (existingOwners.Count() != 1)
            throw new InvalidOperationException("An unexpected error occurred while updating board. Board has more or less than one owner!");
        if (existingOwners.First() != owners.First() && existingOwners.First().UserId != userId)
            throw new ForbidenException("User is not authorized to change the owner of this board.");
        board.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<Board>.Filter.Eq(b => b.Id, board.Id);
        var update = Builders<Board>.Update
            .Set(b => b.Title, board.Title)
            .Set(b => b.Members, board.Members)
            .Set(b => b.UpdatedAt, board.UpdatedAt);
        UpdateResult result = await _boardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0)
            throw new RecordDoesNotExist("Board has not been found.");
    }

    public async Task<BoardRole?> CheckUserBoardRoleAsync(ObjectId id, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var members = await _boardsCollection
            .AsQueryable()
            .Where(x => x.Id == id)
            .Select(x => x.Members)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        return members.FirstOrDefault(x => x.UserId == userId)?.Role;
    }

    public async Task DeleteBoardAsync(ObjectId id, ObjectId userId, CancellationToken cancellationToken = default)
    {
        BoardRole? role = await CheckUserBoardRoleAsync(id, userId, cancellationToken);
        if (role == null || role != BoardRole.Owner)
            throw new ForbidenException("User is not authorized to delete this board.");
        DeleteResult result = await _boardsCollection.DeleteOneAsync(x => x.Id == id, cancellationToken);
        await _cardsCollection.DeleteManyAsync(x => x.BoardId == id, cancellationToken);
        if (result.DeletedCount == 0)
            throw new RecordDoesNotExist("Board has not been found.");
    }
}
