using Boardly.Api.Entities;
using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using Boardly.Api.Models.Dtos;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using MongoDB.Driver.Search;
using System.Collections.Concurrent;

namespace Boardly.Api.Services;

public class CardService
{
    private readonly IMongoCollection<Card> _cardsCollection;
    private readonly IMongoCollection<Board> _boardsCollection;
    public ConcurrentDictionary<ObjectId, ObjectId> CardEditingUsers { get; } = [];
    private readonly MongoClient _mongoClient;

    public CardService(MongoDbProvider mongoDbProvider)
    {
        _cardsCollection = mongoDbProvider.GetCardsCollection();
        _boardsCollection = mongoDbProvider.GetBoardsCollection();
        _mongoClient = mongoDbProvider.Client;
    }

    public async Task<IEnumerable<Card>> GetRawCardsByBoardIdAsync(ObjectId boardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var member = await _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .Select(b => b.Members.FirstOrDefault(m => m.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("User is not a member of this board.");
        return await _cardsCollection.Find(x => x.BoardId == boardId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CardWithAssignedUserAndTags>> GetCardsByBoardIdAsync(ObjectId boardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var member = await _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .Select(b => b.Members.FirstOrDefault(m => m.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("User is not a member of this board.");
        var pipeline = new []
        {
            new BsonDocument("$match", new BsonDocument("boardId", boardId)),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$assignedUsers" },
                { "preserveNullAndEmptyArrays", true }
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "users" },
                { "localField", "assignedUsers" },
                { "foreignField", "_id" },
                { "as", "user" }
            }),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$user" },
                { "preserveNullAndEmptyArrays", true }
            }),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$tags" },
                { "preserveNullAndEmptyArrays", true }
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "boards" },
                { "localField", "boardId" },
                { "foreignField", "_id" },
                { "as", "board" }
            }),
            new BsonDocument("$unwind", new BsonDocument("path", "$board")),
            new BsonDocument("$addFields", new BsonDocument("swimlane",
                new BsonDocument("$first", new BsonDocument("$filter", new BsonDocument
            {
                { "input", "$board.swimlanes" },
                { "as", "sw" },
                { "cond", new BsonDocument("$eq", new BsonArray
                    {
                        "$$sw._id",
                        "$swimlaneId"
                    }) }
            })))),
            new BsonDocument("$addFields", new BsonDocument("tag", new BsonDocument("$first",
                new BsonDocument("$filter", new BsonDocument
            {
                { "input", "$swimlane.tags" },
                { "as", "tg" },
                { "cond", new BsonDocument("$eq", new BsonArray
                    {
                        "$$tg._id",
                        "$tags"
                    }) }
            })))),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$_id" },
                { "boardId", new BsonDocument("$first", "$boardId") },
                { "swimlaneId", new BsonDocument("$first", "$swimlaneId") },
                { "listId", new BsonDocument("$first", "$listId") },
                { "title", new BsonDocument("$first", "$title") },
                { "description", new BsonDocument("$first", "$description") },
                { "dueDate", new BsonDocument("$first", "$dueDate") },
                { "createdAt", new BsonDocument("$first", "$createdAt") },
                { "updatedAt", new BsonDocument("$first", "$updatedAt") },
                { "tags", new BsonDocument("$push", new BsonDocument("$cond", new BsonArray
                    { new BsonDocument("$ne", new BsonArray
                        {
                            new BsonDocument("$type", "$tag"),
                            "missing"
                        }),
                        new BsonDocument
                        {
                            { "_id", "$tag._id" },
                            { "title", "$tag.title" },
                            { "color", "$tag.color" }
                        },
                        "$$REMOVE"
                    }))
                },
                { "assignedUsers", new BsonDocument("$push", new BsonDocument("$cond", new BsonArray
                    { new BsonDocument("$ne", new BsonArray
                        {
                            new BsonDocument("$type", "$user"),
                            "missing"
                        }),
                        new BsonDocument
                        {
                            { "_id", "$user._id" },
                            { "nickname", "$user.nickname" }
                        },
                        "$$REMOVE"
                    }))
                }
            })
        };
        var documents = await _cardsCollection
            .Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        return [.. documents.Select(doc => new CardWithAssignedUserAndTags
        {
            Id = doc["_id"].AsObjectId,
            BoardId = doc["boardId"].AsObjectId,
            SwimlaneId = doc["swimlaneId"].AsObjectId,
            ListId = doc["listId"].AsObjectId,
            Title = doc["title"].AsString,
            Description = doc["description"].IsString ? doc["description"].AsString : null,
            DueDate = doc["dueDate"].ToNullableUniversalTime(),
            CreatedAt = doc["createdAt"].ToUniversalTime(),
            UpdatedAt = doc["updatedAt"].ToUniversalTime(),
            Tags = [.. doc["tags"].AsBsonArray.Select(t =>
            {
                var tag = t.AsBsonDocument;
                return new Entities.Board.Tag
                {
                    Id = tag["_id"].AsObjectId,
                    Title = tag["title"].AsString,
                    Color = Enum.Parse<Color>(tag["color"].AsString)
                };
            })],
            AssignedUsers = [.. doc["assignedUsers"].AsBsonArray.Select(m =>
            {
                var member = m.AsBsonDocument;
                return new AssignedUser
                {
                    Id = member["_id"].AsObjectId,
                    Nickname = member["nickname"].AsString
                };
            })]
        })];
    }

    public async Task<Card?> GetRawCardByIdAsync(ObjectId boardId, ObjectId cardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var member = await _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .Select(b => b.Members.FirstOrDefault(m => m.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("User is not a member of this board.");
        return await _cardsCollection.Find(x => x.Id == cardId && x.BoardId == boardId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CardWithAssignedUserAndTags?> GetCardByIdAsync(ObjectId boardId, ObjectId cardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var member = await _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == boardId)
            .Select(b => b.Members.FirstOrDefault(m => m.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ForbiddenException("User is not a member of this board.");
        var pipeline = new []
        {
            new BsonDocument("$match", new BsonDocument("_id", cardId)),
            new BsonDocument("$match", new BsonDocument("boardId", boardId)),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$assignedUsers" },
                { "preserveNullAndEmptyArrays", true }
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "users" },
                { "localField", "assignedUsers" },
                { "foreignField", "_id" },
                { "as", "user" }
            }),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$user" },
                { "preserveNullAndEmptyArrays", true }
            }),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$tags" },
                { "preserveNullAndEmptyArrays", true }
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "boards" },
                { "localField", "boardId" },
                { "foreignField", "_id" },
                { "as", "board" }
            }),
            new BsonDocument("$unwind", new BsonDocument("path", "$board")),
            new BsonDocument("$addFields", new BsonDocument("swimlane",
                new BsonDocument("$first", new BsonDocument("$filter", new BsonDocument
            {
                { "input", "$board.swimlanes" },
                { "as", "sw" },
                { "cond", new BsonDocument("$eq", new BsonArray
                    {
                        "$$sw._id",
                        "$swimlaneId"
                    }) }
            })))),
            new BsonDocument("$addFields", new BsonDocument("tag", new BsonDocument("$first",
                new BsonDocument("$filter", new BsonDocument
            {
                { "input", "$swimlane.tags" },
                { "as", "tg" },
                { "cond", new BsonDocument("$eq", new BsonArray
                    {
                        "$$tg._id",
                        "$tags"
                    }) }
            })))),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$_id" },
                { "boardId", new BsonDocument("$first", "$boardId") },
                { "swimlaneId", new BsonDocument("$first", "$swimlaneId") },
                { "listId", new BsonDocument("$first", "$listId") },
                { "title", new BsonDocument("$first", "$title") },
                { "description", new BsonDocument("$first", "$description") },
                { "dueDate", new BsonDocument("$first", "$dueDate") },
                { "createdAt", new BsonDocument("$first", "$createdAt") },
                { "updatedAt", new BsonDocument("$first", "$updatedAt") },
                { "tags", new BsonDocument("$push", new BsonDocument("$cond", new BsonArray
                    { new BsonDocument("$ne", new BsonArray
                        {
                            new BsonDocument("$type", "$tag"),
                            "missing"
                        }),
                        new BsonDocument
                        {
                            { "_id", "$tag._id" },
                            { "title", "$tag.title" },
                            { "color", "$tag.color" }
                        },
                        "$$REMOVE"
                    }))
                },
                { "assignedUsers", new BsonDocument("$push", new BsonDocument("$cond", new BsonArray
                    { new BsonDocument("$ne", new BsonArray
                        {
                            new BsonDocument("$type", "$user"),
                            "missing"
                        }),
                        new BsonDocument
                        {
                            { "_id", "$user._id" },
                            { "nickname", "$user.nickname" }
                        },
                        "$$REMOVE"
                    }))
                }
            })
        };
        var doc = await _cardsCollection
            .Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken)
            .FirstOrDefaultAsync(cancellationToken);
        return new CardWithAssignedUserAndTags
        {
            Id = doc["_id"].AsObjectId,
            BoardId = doc["boardId"].AsObjectId,
            SwimlaneId = doc["swimlaneId"].AsObjectId,
            ListId = doc["listId"].AsObjectId,
            Title = doc["title"].AsString,
            Description = doc["description"].IsString ? doc["description"].AsString : null,
            DueDate = doc["dueDate"].ToNullableUniversalTime(),
            CreatedAt = doc["createdAt"].ToUniversalTime(),
            UpdatedAt = doc["updatedAt"].ToUniversalTime(),
            Tags = [.. doc["tags"].AsBsonArray.Select(t =>
            {
                var tag = t.AsBsonDocument;
                return new Entities.Board.Tag
                {
                    Id = tag["_id"].AsObjectId,
                    Title = tag["title"].AsString,
                    Color = Enum.Parse<Color>(tag["color"].AsString)
                };
            })],
            AssignedUsers = [.. doc["assignedUsers"].AsBsonArray.Select(m =>
            {
                var member = m.AsBsonDocument;
                return new AssignedUser
                {
                    Id = member["_id"].AsObjectId,
                    Nickname = member["nickname"].AsString
                };
            })]
        };
    }

    public bool StartCardEditing(ObjectId cardId, ObjectId userId) =>
        CardEditingUsers.TryAdd(cardId, userId);

    public bool ForceEndCardEditing(ObjectId cardId) =>
        CardEditingUsers.TryRemove(cardId, out _);

    public async Task FinishCardEditing(Card card, ObjectId userId, CancellationToken cancellationToken = default)
    {
        if (!CardEditingUsers.TryGetValue(card.Id, out ObjectId value) || value != userId)
            throw new InvalidOperationException("User is not editing this card.");
        await UpdateCardAsync(card, userId, cancellationToken);
        CardEditingUsers.TryRemove(card.Id, out _);
    }

    public async Task CreateCardAsync(Card card, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == card.BoardId)
            .Select(b => new
            {
                b.Members,
                List = b.Swimlanes.Where(s => s.Id == card.SwimlaneId).Select(s => s.Lists.FirstOrDefault(l => l.Id == card.ListId)).FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        Member? member = result.Members.FirstOrDefault(x => x.UserId == userId);
        if (member?.Role == BoardRole.Viewer)
            throw new ForbiddenException("User does not have permission to create card.");
        if (card.AssignedUsers.Any(x => !result.Members.Any(m => m.UserId == x)))
            throw new ForbiddenException("One or more assigned users are not members of this board.");
        if (result.List == null)
            throw new RecordDoesNotExist("List has not been found.");
        if (result.List.MaxWIP.HasValue && result.List.MaxWIP.Value > 0)
        {
            long currentWIP = await _cardsCollection.CountDocumentsAsync(c => c.BoardId == card.BoardId
                && c.SwimlaneId == card.SwimlaneId && c.ListId == card.ListId, cancellationToken: cancellationToken);
            if (currentWIP >= result.List.MaxWIP.Value)
                throw new ForbiddenException("Maximum cards WIP limit reached for this list.");
        }
        card.CreatedAt = DateTime.UtcNow;
        card.UpdatedAt = DateTime.UtcNow;
        await _cardsCollection.InsertOneAsync(card, cancellationToken: cancellationToken);
    }

    public async Task UpdateCardAsync(Card card, ObjectId userId, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(b => b.Id == card.BoardId)
            .Select(b => new
            {
                b.Members,
                MaxWIP = b.Swimlanes
                    .Where(s => s.Id == card.SwimlaneId)
                    .Select(s => s.Lists
                        .Where(l => l.Id == card.ListId)
                        .Select(l => l.MaxWIP)
                        .FirstOrDefault())
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        BoardRole role = result.Members.FirstOrDefault(x => x.UserId == userId)?.Role
            ?? throw new ForbiddenException("User is not a member of this board.");
        if (role == BoardRole.Viewer)
            throw new ForbiddenException("User does not have permission to create card.");
        if (card.AssignedUsers.Any(x => !result.Members.Any(m => m.UserId == x)))
            throw new ForbiddenException("One or more assigned users are not members of this board.");
        var oldCard = await _cardsCollection
            .AsQueryable()
            .Where(x => x.Id == card.Id)
            .Select(x => new { x.ListId, x.UpdatedAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (result.MaxWIP.HasValue && result.MaxWIP.Value > 0)
        {
            long currentWIP = await _cardsCollection.CountDocumentsAsync(x => x.BoardId == card.BoardId
                && x.ListId == card.ListId, cancellationToken: cancellationToken);
            if (oldCard.ListId != card.ListId && currentWIP >= result.MaxWIP.Value)
                throw new ForbiddenException("Maximum WIP limit reached for this list.");
        }

        if (card.UpdatedAt != default && oldCard.UpdatedAt != card.UpdatedAt)
            throw new PreconditionFailedException("Card has been modified or deleted by another user since it was last read.");
        var now = DateTime.UtcNow;
        var update = Builders<Card>.Update
            .Set(c => c.ListId, card.ListId)
            .Set(c => c.Title, card.Title)
            .Set(c => c.Description, card.Description)
            .Set(c => c.DueDate, card.DueDate)
            .Set(c => c.Tags, card.Tags)
            .Set(c => c.AssignedUsers, card.AssignedUsers)
            .Set(c => c.UpdatedAt, now);
        var filter = Builders<Card>.Filter.Eq(c => c.Id, card.Id);
        if (card.UpdatedAt != default)
            filter &= Builders<Card>.Filter.Eq(c => c.UpdatedAt, card.UpdatedAt);
        UpdateResult updateResult = await _cardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        if (updateResult.MatchedCount == 0 && card.UpdatedAt != default)
            throw new PreconditionFailedException("Card has been modified or deleted by another user by another user since it was last read.");
        else if (updateResult.MatchedCount != 0 && updateResult.ModifiedCount == 0)
            throw new InvalidOperationException("An unexpected error occurred while updating card. Card has not been modified.");
        card.UpdatedAt = now;
    }

    public async Task<DateTime> MoveCardAsync(ObjectId cardId, ObjectId userId, ObjectId listId, 
        DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ctx) =>
        {
            var result = await _cardsCollection
                .AsQueryable()
                .Where(card => card.Id == cardId)
                .Join(
                    _boardsCollection,
                    c => c.BoardId,
                    b => b.Id,
                    (c, b) => new
                    {
                        c.UpdatedAt,
                        CardAssignedUsers = c.AssignedUsers,
                        BoardMembers = b.Members,
                        BoardId = b.Id,
                        MaxWIP = b.Swimlanes
                            .Where(s => s.Id == c.SwimlaneId)
                            .Select(s => s.Lists
                                .Where(l => l.Id == listId)
                                .Select(l => l.MaxWIP)
                                .FirstOrDefault())
                            .FirstOrDefault()
                    }
                )
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new RecordDoesNotExist("Card has not been found");
            if (updatedAt != default && updatedAt != result.UpdatedAt)
                throw new PreconditionFailedException("Board has been modified by another user since it was last read.");
            var member = result.BoardMembers.FirstOrDefault(m => m.UserId == userId)
                ?? throw new ForbiddenException("User is not a member of this board.");
            if (member.Role == BoardRole.Viewer && !result.CardAssignedUsers.Contains(userId))
                throw new ForbiddenException("User does not have permission to move this card");
            if (result.MaxWIP.HasValue && result.MaxWIP.Value > 0)
            {
                long currentWIP = await _cardsCollection.CountDocumentsAsync(x => x.BoardId == result.BoardId
                    && x.ListId == listId, cancellationToken: cancellationToken);
                var oldListId = await _cardsCollection
                    .AsQueryable()
                    .Where(x => x.Id == cardId)
                    .Select(x => x.ListId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (oldListId != listId && currentWIP >= result.MaxWIP.Value)
                    throw new ForbiddenException("Maximum WIP limit reached for this list.");
            }
            var filter = Builders<Card>.Filter.Eq(c => c.Id, cardId);
            if (updatedAt != default)
                filter &= Builders<Card>.Filter.Eq(c => c.UpdatedAt, updatedAt);
            var now = DateTime.UtcNow;
            var update = Builders<Card>.Update
                .Set(c => c.ListId, listId)
                .Set(c => c.UpdatedAt, now);
            UpdateResult updateResult = await _cardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
            if (updateResult.MatchedCount == 0 && updatedAt != default)
                throw new PreconditionFailedException("Card has been modified by another user since it was last read.");
            else if (updateResult.MatchedCount != 0 && updateResult.ModifiedCount == 0)
                throw new InvalidOperationException("An unexpected error occurred while moving card. Card has not been modified.");
            return now;
        }, cancellationToken: cancellationToken);

    }

    public async Task DeleteCardAsync(ObjectId cardId, ObjectId userId, DateTime updatedAt = default, CancellationToken cancellationToken = default)
    {
        var result = await _cardsCollection
            .AsQueryable()
            .Where(card => card.Id == cardId)
            .Join(
                _boardsCollection,
                c => c.BoardId,
                b => b.Id,
                (c, b) => new
                {
                    c.UpdatedAt,
                    CardId = c.Id,
                    BoardRole = b.Members
                        .Where(x => x.UserId == userId)
                        .Select(x => x.Role)
                        .FirstOrDefault(),
                }
            )
            .FirstOrDefaultAsync(cancellationToken);
        if (result == null || result.CardId == default)
            throw new RecordDoesNotExist("Card has not been found.");
        if (updatedAt != default && updatedAt != result.UpdatedAt)
            throw new PreconditionFailedException("Board has been modified or deleted by another user since it was last read.");
        if (result.BoardRole == BoardRole.Viewer)
            throw new ForbiddenException("User does not have permission to delete card.");
        var filter = Builders<Card>.Filter.Eq(c => c.Id, cardId);
        if (updatedAt != default)
            filter &= Builders<Card>.Filter.Eq(c => c.UpdatedAt, updatedAt);
        DeleteResult deleteResult = await _cardsCollection.DeleteOneAsync(filter, null, cancellationToken: cancellationToken);
        if (deleteResult.DeletedCount == 0 && updatedAt != default)
            throw new PreconditionFailedException("Card has been modified or deleted by another user since it was last read.");
    }
}
