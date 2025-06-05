using Boardly.Api.Entities;
using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using Boardly.Api.Models.Dtos;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Boardly.Api.Services;

public class CardService
{
    private readonly IMongoCollection<Card> _cardsCollection;
    private readonly BoardService _boardService;
    private readonly IMongoCollection<Board> _boardsCollection;

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
            Description = doc["description"].AsString,
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

    public async Task<Card?> GetRawCardByIdAsync(ObjectId cardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        if (await _boardService.CheckUserBoardRoleAsync(cardId, userId, cancellationToken) == null)
            throw new ForbidenException("User is not a member of this board.");
        return await _cardsCollection.Find(x => x.Id == cardId).FirstOrDefaultAsync(cancellationToken);
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
            Description = doc["description"].AsString,
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
        BoardRole role = await _boardService.CheckUserBoardRoleAsync(card.BoardId, userId, cancellationToken)
             ?? throw new ForbidenException("User is not a member of this board.");
        if (role == BoardRole.Viewer)
            throw new ForbidenException("User does not have permission to create card.");
        await _cardsCollection.InsertOneAsync(card, cancellationToken: cancellationToken);
    }

    public async Task UpdateCardAsync(ObjectId cardId, ObjectId userId, Card card, CancellationToken cancellationToken = default)
    {
        BoardRole role = await _boardService.CheckUserBoardRoleAsync(card.BoardId, userId, cancellationToken)
             ?? throw new ForbidenException("User is not a member of this board.");
        if (role == BoardRole.Viewer)
            throw new ForbidenException("User does not have permission to update card.");
        
        var filter = Builders<Card>.Filter.Eq(x => x.Id, cardId); 
        var update = Builders<Card>.Update
            .Set(c => c.SwimlaneId, card.SwimlaneId)
            .Set(c => c.ListId, card.ListId)
            .Set(c => c.Title, card.Title)
            .Set(c => c.Description, card.Description)
            .Set(c => c.DueDate, card.DueDate)
            .Set(c => c.Tags, card.Tags)
            .Set(c => c.AssignedUsers, card.AssignedUsers)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);
            
        await _cardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task MoveCardAsync(ObjectId cardId, ObjectId userId, ObjectId listId,
        CancellationToken cancellationToken = default)
    {
        Card card = await _cardsCollection.Find(x => x.Id == cardId).FirstOrDefaultAsync(cancellationToken)
                    ?? throw new RecordDoesNotExist("Card has not been found.");
        Board board = await _boardsCollection.Find(x => x.Id == card.BoardId).FirstOrDefaultAsync(cancellationToken) 
                      ?? throw new RecordDoesNotExist("Board has not been found.");
        Member member = board.Members.FirstOrDefault(x => x.UserId == userId) 
                        ?? throw new ForbidenException("User is not a member of this board.");
        if (member.Role == BoardRole.Viewer && !card.AssignedUsers.Contains(member.UserId))
        {
            throw new ForbidenException("User does not have permission to move this card");
        }
        var filter = Builders<Card>.Filter.Eq(x => x.Id, cardId);
        var update = Builders<Card>.Update
            .Set(c => c.ListId, listId);
        await _cardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task DeleteCardAsync(ObjectId cardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        Card card = await _cardsCollection.Find(x => x.Id == cardId).FirstOrDefaultAsync(cancellationToken)
                    ?? throw new RecordDoesNotExist("Card has not been found.");
        BoardRole role = await _boardService.CheckUserBoardRoleAsync(card.BoardId, userId, cancellationToken)
             ?? throw new ForbidenException("User is not a member of this board.");
        if (role == BoardRole.Viewer)
            throw new ForbidenException("User does not have permission to delete card.");
        await _cardsCollection.DeleteOneAsync(x => x.Id == cardId, null, cancellationToken: cancellationToken);
    }
}
