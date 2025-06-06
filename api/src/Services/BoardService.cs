using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using Boardly.Api.Models.Dtos;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Boardly.Api.Services;

public class BoardService
{
    private readonly IMongoCollection<Board> _boardsCollection;
    private readonly IMongoCollection<Card> _cardsCollection;

    public BoardService(MongoDbProvider mongoDbProvider)
    {
        _boardsCollection = mongoDbProvider.GetBoardsCollection();
        _cardsCollection = mongoDbProvider.GetCardsCollection();
    }

    public async Task CreateBoardAsync(Board board, CancellationToken cancellationToken = default)
    {
        if (board.Members.Count(x => x.Role == BoardRole.Owner) != 1)
            throw new ArgumentException("Board must have at least one member that is an owner.");
        await _boardsCollection.InsertOneAsync(board, cancellationToken: cancellationToken);
    }

    public async Task<Board?> GetRawBoardByIdAsync(ObjectId id, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var board = await _boardsCollection.Find(x => x.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (board == null)
            return null;
        if (board.Members.All(x => x.UserId != userId))
            throw new ForbidenException("User is not a member of this board.");
        return board;
    }

    public async Task<BoardWithUser?> GetBoardByIdAsync(ObjectId id, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("_id", id)),
            new BsonDocument("$match", new BsonDocument("members.userId", userId)),
            new BsonDocument("$unwind", "$members"),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "users" },
                { "localField", "members.userId" },
                { "foreignField", "_id" },
                { "as", "user" }
            }),
            new BsonDocument("$unwind", "$user"),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$_id" },
                { "title", new BsonDocument("$first", "$title") },
                { "swimlanes", new BsonDocument("$first", "$swimlanes") },
                { "createdAt", new BsonDocument("$first", "$createdAt") },
                { "updatedAt", new BsonDocument("$first", "$updatedAt") },
                { "members", new BsonDocument("$push", new BsonDocument
                    {
                        { "userId", "$members.userId" },
                        { "role", "$members.role" },
                        { "isActive", "$members.isActive" },
                        { "nickname", "$user.nickname" }
                    })
                }
            })
        };

        var doc = await _boardsCollection
            .Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken)
            .FirstOrDefaultAsync(cancellationToken) ?? throw new ForbidenException("User is not a member of this board.");
        if (doc == null)
            return null;
        BoardWithUser board = new()
        {
            Id = doc["_id"].AsObjectId,
            Title = doc["title"].AsString,
            Swimlanes = [.. doc["swimlanes"].AsBsonArray.Select(s => BsonSerializer.Deserialize<Swimlane>(s.AsBsonDocument))],
            CreatedAt = doc["createdAt"].ToUniversalTime(),
            UpdatedAt = doc["updatedAt"].ToUniversalTime(),
            Members = [.. doc["members"].AsBsonArray.Select(m =>
            {
                var member = m.AsBsonDocument;
                return new MemberWithUser
                {
                    UserId = member["userId"].AsObjectId,
                    Role = Enum.Parse<BoardRole>(member["role"].AsString),
                    IsActive = member["isActive"].AsBoolean,
                    Nickname = member["nickname"].AsString
                };
            })]
        };
        if (board.Members.All(x => x.UserId != userId))
            throw new ForbidenException("User is not a member of this board.");
        return board;
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
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("members.userId", userId)),
            new BsonDocument("$unwind", "$members"),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "users" },
                { "localField", "members.userId" },
                { "foreignField", "_id" },
                { "as", "user" }
            }),
            new BsonDocument("$unwind", "$user"),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$_id" },
                { "title", new BsonDocument("$first", "$title") },
                { "swimlanes", new BsonDocument("$first", "$swimlanes") },
                { "createdAt", new BsonDocument("$first", "$createdAt") },
                { "updatedAt", new BsonDocument("$first", "$updatedAt") },
                { "members", new BsonDocument("$push", new BsonDocument
                    {
                        { "userId", "$members.userId" },
                        { "role", "$members.role" },
                        { "isActive", "$members.isActive" },
                        { "nickname", "$user.nickname" }
                    })
                }
            })
        };

        var documents = await _boardsCollection
            .Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        return [.. documents.Select(doc => new BoardWithUser
        {
            Id = doc["_id"].AsObjectId,
            Title = doc["title"].AsString,
            Swimlanes = [.. doc["swimlanes"].AsBsonArray.Select(s => BsonSerializer.Deserialize<Swimlane>(s.AsBsonDocument))],
            CreatedAt = doc["createdAt"].ToUniversalTime(),
            UpdatedAt = doc["updatedAt"].ToUniversalTime(),
            Members = [.. doc["members"].AsBsonArray.Select(m =>
            {
                var member = m.AsBsonDocument;
                return new MemberWithUser
                {
                    UserId = member["userId"].AsObjectId,
                    Role = Enum.Parse<BoardRole>(member["role"].AsString),
                    IsActive = member["isActive"].AsBoolean,
                    Nickname = member["nickname"].AsString
                };
            })]
        })];
    }

    public async Task<IEnumerable<Board>> GetRawBoardsByUserIdAsync(ObjectId userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Board>.Filter.ElemMatch(b => b.Members, m => m.UserId == userId);
        return await _boardsCollection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task UpdateBoardAsync(Board board, ObjectId userId, CancellationToken cancellationToken = default)
    {
        IEnumerable<Member> owners = board.Members.Where(x => x.Role == BoardRole.Owner);
        if (owners.Count() != 1)
            throw new ArgumentException("Board must have at least one member that is an owner.");
        var existingBoard = await _boardsCollection.Find(b => b.Id == board.Id, null).FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        BoardRole? role = existingBoard.Members.FirstOrDefault(x => x.UserId == userId)?.Role;
        if (role == null || (role != BoardRole.Owner && role != BoardRole.Admin))
            throw new ForbidenException("User is not authorized to update this board.");
        IEnumerable<Member> existingOwners = existingBoard.Members.Where(x => x.Role == BoardRole.Owner);
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
        HashSet<Member>? members = null;
        members = await _boardsCollection.Find(b => b.Id == id).Project(x => x.Members).FirstOrDefaultAsync(cancellationToken);
        if (members == null)
            throw new RecordDoesNotExist("Board has not been found.");
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
