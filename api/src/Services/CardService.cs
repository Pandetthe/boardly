using Boardly.Api.Entities;
using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using Boardly.Api.Models.Dtos;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Boardly.Api.Services;

public class CardService
{
    private readonly IMongoCollection<Card> _cardsCollection;
    private readonly IMongoCollection<Board> _boardsCollection;
    private readonly BoardService _boardService;

    public CardService(MongoDbProvider mongoDbProvider, BoardService boardService)
    {
        _cardsCollection = mongoDbProvider.GetCardsCollection();
        _boardService = boardService;
        _boardsCollection = mongoDbProvider.GetBoardsCollection();
    }

    public async Task<IEnumerable<Card>> GetRawCardsByBoardIdAsync(ObjectId boardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        if (await _boardService.CheckUserBoardRoleAsync(boardId, userId, cancellationToken) == null)
            throw new ForbidenException("User is not a member of this board.");
        return await _cardsCollection.Find(x => x.BoardId == boardId).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CardWithAssignedUserAndTags>> GetCardsByBoardIdAsync(ObjectId boardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        if (await _boardService.CheckUserBoardRoleAsync(boardId, userId, cancellationToken) == null)
            throw new ForbidenException("User is not a member of this board.");
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
        if (await _boardService.CheckUserBoardRoleAsync(boardId, userId, cancellationToken) == null)
            throw new ForbidenException("User is not a member of this board.");
        return await _cardsCollection.Find(x => x.Id == cardId && x.BoardId == boardId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CardWithAssignedUserAndTags?> GetCardByIdAsync(ObjectId boardId, ObjectId cardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        if (await _boardService.CheckUserBoardRoleAsync(boardId, userId, cancellationToken) == null)
            throw new ForbidenException("User is not a member of this board.");
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

    public async Task CreateCardAsync(ObjectId userId, Card card, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(x => x.Id == card.BoardId)
            .Select(x => new
            {
                x.Members,
                MaxWIP = x.Swimlanes
                    .Where(s => s.Id == card.SwimlaneId)
                    .Select(s => s.Lists
                        .Where(l => l.Id == card.ListId)
                        .Select(l => l.MaxWIP)
                        .FirstOrDefault())
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        Member member = result.Members.FirstOrDefault(x => x.UserId == userId)
            ?? throw new ForbidenException("User is not a member of this board.");
        if (member.Role == BoardRole.Viewer)
            throw new ForbidenException("User does not have permission to create card.");
        if (card.AssignedUsers.Any(x => !result.Members.Any(m => m.UserId == x)))
            throw new ForbidenException("One or more assigned users are not members of this board.");
        if (result.MaxWIP.HasValue && result.MaxWIP.Value > 0)
        {
            long currentWIP = await _cardsCollection.CountDocumentsAsync(x => x.BoardId == card.BoardId
                && x.ListId == card.ListId, cancellationToken: cancellationToken);
            if (currentWIP >= result.MaxWIP.Value)
                throw new ForbidenException("Maximum WIP limit reached for this list.");
        }
        await _cardsCollection.InsertOneAsync(card, cancellationToken: cancellationToken);
    }

    public async Task UpdateCardAsync(ObjectId cardId, ObjectId userId, Card card, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable()
            .Where(x => x.Id == card.BoardId)
            .Select(x => new
            {
                x.Members,
                MaxWIP = x.Swimlanes
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
            ?? throw new ForbidenException("User is not a member of this board.");
        if (role == BoardRole.Viewer)
            throw new ForbidenException("User does not have permission to create card.");
        if (card.AssignedUsers.Any(x => !result.Members.Any(m => m.UserId == x)))
            throw new ForbidenException("One or more assigned users are not members of this board.");
        if (result.MaxWIP.HasValue && result.MaxWIP.Value > 0)
        {
            long currentWIP = await _cardsCollection.CountDocumentsAsync(x => x.BoardId == card.BoardId
                && x.ListId == card.ListId, cancellationToken: cancellationToken);
            var oldListId = await _cardsCollection
                .AsQueryable()
                .Where(x => x.Id == cardId)
                .Select(x => x.ListId)
                .FirstOrDefaultAsync(cancellationToken);
            if (oldListId != card.ListId && currentWIP >= result.MaxWIP.Value)
                throw new ForbidenException("Maximum WIP limit reached for this list.");
        }
        card.UpdatedAt = DateTime.UtcNow;
        var update = Builders<Card>.Update
            .Set(c => c.ListId, card.ListId)
            .Set(c => c.Title, card.Title)
            .Set(c => c.Description, card.Description)
            .Set(c => c.DueDate, card.DueDate)
            .Set(c => c.Tags, card.Tags)
            .Set(c => c.AssignedUsers, card.AssignedUsers)
            .Set(c => c.UpdatedAt, card.UpdatedAt);
        await _cardsCollection.UpdateOneAsync(x => x.Id == cardId, update, cancellationToken: cancellationToken);
    }

    public async Task MoveCardAsync(ObjectId cardId, ObjectId userId, ObjectId listId, CancellationToken cancellationToken = default)
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
        var member = result.BoardMembers.FirstOrDefault(m => m.UserId == userId)
            ?? throw new ForbidenException("User is not a member of this board.");
        if (member.Role == BoardRole.Viewer && !result.CardAssignedUsers.Contains(userId))
            throw new ForbidenException("User does not have permission to move this card");
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
                throw new ForbidenException("Maximum WIP limit reached for this list.");
        }
        var update = Builders<Card>.Update
            .Set(c => c.ListId, listId);
        await _cardsCollection.UpdateOneAsync(x => x.Id == cardId, update, cancellationToken: cancellationToken);
    }

    public async Task DeleteCardAsync(ObjectId cardId, ObjectId userId, CancellationToken cancellationToken = default)
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
        if (result.BoardRole == BoardRole.Viewer)
            throw new ForbidenException("User does not have permission to delete card.");
        await _cardsCollection.DeleteOneAsync(x => x.Id == cardId, null, cancellationToken: cancellationToken);
    }
}
