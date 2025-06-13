using Boardly.Api.Entities;
using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using Boardly.Api.Models.Dtos;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Boardly.Api.Services;

public class BoardService
{
    private readonly IMongoCollection<Board> _boardsCollection;
    private readonly IMongoCollection<Card> _cardsCollection;
    private readonly IMongoCollection<User> _usersCollection;
    private readonly MongoClient _mongoClient;

    public BoardService(MongoDbProvider mongoDbProvider)
    {
        _boardsCollection = mongoDbProvider.GetBoardsCollection();
        _cardsCollection = mongoDbProvider.GetCardsCollection();
        _usersCollection = mongoDbProvider.GetUsersCollection();
        _mongoClient = mongoDbProvider.Client;
    }

    public IQueryable<Board> GetRawBoardsByUserId(ObjectId userId)
    {
        return _boardsCollection
            .AsQueryable()
            .Where(b => b.Members.Any(m => m.UserId == userId));
    }

    public IQueryable<BoardWithUser> GetBoardsByUserId(ObjectId userId)
    {
        return _boardsCollection
            .AsQueryable()
            .Where(b => b.Members.Any(m => m.UserId == userId))
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
            .Select(x => new BoardWithUser
            {
                Id = x.First().Board.Id,
                Title = x.First().Board.Title,
                CreatedAt = x.First().Board.CreatedAt,
                UpdatedAt = x.First().Board.UpdatedAt,
                Swimlanes = x.First().Board.Swimlanes,
                Members = x.Select(m => m.Member),
            });
    }

    public IQueryable<Member> GetBoardMembers(ObjectId boardId)
    {
        return _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .SelectMany(b => b.Members);
    }

    public async Task<BoardRole?> GetUserBoardRoleAsync(ObjectId boardId, ObjectId userId, CancellationToken cancellationToken = default) =>
        (await GetBoardMembers(boardId).FirstOrDefaultAsync(m => m.UserId == userId, cancellationToken))?.Role;

    public async Task CreateBoardAsync(Board board, CancellationToken cancellationToken = default)
    {
        if (board.Members.Count(x => x.Role == BoardRole.Owner) != 1)
            throw new ArgumentException("Board must have at least one member that is an owner.");
        await _boardsCollection.InsertOneAsync(board, cancellationToken: cancellationToken);
    }

    public async Task ChangeUserActivity(ObjectId id, ObjectId userId,
        bool isActive, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Board>.Filter.And(
            Builders<Board>.Filter.Eq(x => x.Id, id),
            Builders<Board>.Filter.ElemMatch(x=> x.Members, Builders<Member>.Filter.Eq(x => x.UserId, userId)));

        var update = Builders<Board>.Update.Set("members.$.isActive", isActive);

        await _boardsCollection.UpdateOneAsync(filter, update, null, cancellationToken);
    }

    public async Task UpdateBoardAsync(Board board, ObjectId userId, IClientSessionHandle session, CancellationToken cancellationToken = default)
    {
        IEnumerable<Member> owners = board.Members.Where(x => x.Role == BoardRole.Owner);
        if (owners.Count() != 1)
            throw new ArgumentException("Board must have exactly one member that is an owner.");
        ObjectId ownerId = owners.Single().UserId;
        var result = await _boardsCollection
            .AsQueryable(session)
            .Where(x => x.Id == board.Id)
            .Select(x => new
            {
                x.Members,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (board.UpdatedAt != default && board.UpdatedAt != result.UpdatedAt)
            throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
        HashSet<Member> members = result.Members;
        BoardRole? role = members.FirstOrDefault(x => x.UserId == userId)?.Role;
        if (role == null || (role != BoardRole.Owner && role != BoardRole.Admin))
            throw new ForbiddenException("User is not authorized to update this board.");
        IEnumerable<Member> existingOwners = members.Where(x => x.Role == BoardRole.Owner);
        if (existingOwners.Count() != 1)
            throw new InvalidOperationException("An unexpected error occurred while updating board. Board has more or less than one owner!");
        ObjectId existingOwnerId = existingOwners.Single().UserId;
        if (existingOwnerId != ownerId && existingOwnerId != userId)
            throw new ForbiddenException("User is not authorized to change the owner of this board.");
        var newTime = DateTime.UtcNow;
        var filter = Builders<Board>.Filter.Eq(b => b.Id, board.Id);
        if (board.UpdatedAt != default)
            filter &= Builders<Board>.Filter.Eq(b => b.UpdatedAt, board.UpdatedAt);
        var update = Builders<Board>.Update
            .Set(b => b.Title, board.Title)
            .Set(b => b.Members, board.Members)
            .Set(b => b.UpdatedAt, newTime);
        UpdateResult updateResult = await _boardsCollection.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken);
        if (updateResult.MatchedCount == 0 && board.UpdatedAt != default)
            throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
        else if (updateResult.MatchedCount != 0 && updateResult.ModifiedCount == 0)
            throw new InvalidOperationException("An unexpected error occurred while updating board. Board has not been modified.");
        board.UpdatedAt = newTime;
    }

    public async Task<BoardWithUser> UpdateAndFindBoardAsync(Board board, ObjectId userId, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ct) =>
        {
            await UpdateBoardAsync(board, userId, s, ct);
            return await GetBoardsByUserId(userId).SingleAsync(b => b.Id == board.Id, cancellationToken);
        }, cancellationToken: cancellationToken);
    }

    public async Task DeleteBoardAsync(ObjectId id, ObjectId userId, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction();
        try
        {
            var result = await _boardsCollection
                .AsQueryable(session)
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    Member = x.Members.FirstOrDefault(m => m.UserId == userId),
                    x.UpdatedAt
                })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Board has not been found.");
            if (updatedAt != default && updatedAt != result.UpdatedAt)
                throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
            if (result.Member?.Role != BoardRole.Owner)
                throw new ForbiddenException("User is not authorized to delete this board.");
            var filter = Builders<Board>.Filter.Eq(b => b.Id, id);
            if (updatedAt != default)
                filter &= Builders<Board>.Filter.Eq(b => b.UpdatedAt, updatedAt);
            DeleteResult deleteResult = await _boardsCollection.DeleteOneAsync(session, filter, cancellationToken: cancellationToken);
            if (deleteResult.DeletedCount == 0 && updatedAt != default)
                throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
            await _cardsCollection.DeleteManyAsync(session, x => x.BoardId == id, cancellationToken: cancellationToken);
            await session.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception)
        {
            await session.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }
}
