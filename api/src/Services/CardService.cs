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
    private readonly IMongoCollection<User> _usersCollection;
    private readonly MongoClient _mongoClient;

    public CardService(MongoDbProvider mongoDbProvider)
    {
        _cardsCollection = mongoDbProvider.GetCardsCollection();
        _boardsCollection = mongoDbProvider.GetBoardsCollection();
        _usersCollection = mongoDbProvider.GetUsersCollection();
        _mongoClient = mongoDbProvider.Client;
    }

    public ConcurrentDictionary<ObjectId, ObjectId> LockedCards { get; } = [];

    public async Task<List<CardWithAssignedUserAndTags>> GetCardsByBoardId(ObjectId boardId, IClientSessionHandle? session = null,
        CancellationToken cancellationToken = default)
    {
        List<CardWithAssignedUserAndTags> data = await (session == null
            ? _cardsCollection.AsQueryable()
            : _cardsCollection.AsQueryable(session))
            .Where(c => c.BoardId == boardId)
            .SelectMany(c => c.AssignedUsers.DefaultIfEmpty(), (card, assignedUserId) => new { card, assignedUserId })
            .GroupJoin(_usersCollection,
                c => c.assignedUserId,
                u => u.Id,
                (x, u) => new { x.card, x.assignedUserId, user = u.FirstOrDefault() })
            .SelectMany(x => x.card.Tags.DefaultIfEmpty(), (x, tagId) => new { x.card, x.assignedUserId, x.user, tagId })
            .Join(_boardsCollection,
                x => x.card.BoardId,
                board => board.Id,
                (x, board) => new { x.card, x.assignedUserId, x.user, x.tagId, board })
            .Select(x => new
            {
                x.card.Id,
                x.card.BoardId,
                x.card.SwimlaneId,
                x.card.ListId,
                x.card.Title,
                x.card.Description,
                x.card.DueDate,
                x.card.CreatedAt,
                x.card.UpdatedAt,
                Tag = x.board.Swimlanes
                    .Where(s => s.Id == x.card.SwimlaneId)
                    .Select(s => s.Tags.FirstOrDefault(t => t.Id == x.tagId))
                    .FirstOrDefault(),
                AssignedUser = x.user == null ? null : new SimplifiedUser
                {
                    Id = x.user.Id,
                    Nickname = x.user.Nickname,
                },
            })
            .GroupBy(x => x.Id)
            .Select(g => new CardWithAssignedUserAndTags
            {
                Id = g.Key,
                BoardId = g.First().BoardId,
                SwimlaneId = g.First().SwimlaneId,
                ListId = g.First().ListId,
                Title = g.First().Title,
                Description = g.First().Description,
                DueDate = g.First().DueDate,
                CreatedAt = g.First().CreatedAt,
                UpdatedAt = g.First().UpdatedAt,
                Tags = g.Where(x => x.Tag != null).Select(x => x.Tag!),
                AssignedUsers = g.Where(x => x.AssignedUser != null).Select(x => x.AssignedUser!),
            }).ToListAsync(cancellationToken: cancellationToken);
        var lockedUserIds = LockedCards.Values.Distinct().ToList();
        var lockedUsers = await GetSimplifiedUser(session).Where(u => lockedUserIds.Contains(u.Id)).ToListAsync(cancellationToken);
        var lockedUsersDict = lockedUsers.ToDictionary(u => u.Id, u => u);
        foreach (var card in data)
        {
            if (LockedCards.TryGetValue(card.Id, out var lockedUserId) && lockedUsersDict.TryGetValue(lockedUserId, out var lockedUser))
            {
                card.LockedByUser = new SimplifiedUser
                {
                    Id = lockedUser.Id,
                    Nickname = lockedUser.Nickname
                };
            }
        }
        return data;
    }

    public async Task<CardWithAssignedUserAndTags?> GetCardById(ObjectId cardId, ObjectId boardId,
        IClientSessionHandle? session = null, CancellationToken cancellationToken = default)
    {
        CardWithAssignedUserAndTags? data = await (session == null
            ? _cardsCollection.AsQueryable()
            : _cardsCollection.AsQueryable(session))
            .Where(c => c.BoardId == boardId && c.Id == cardId)
            .SelectMany(c => c.AssignedUsers.DefaultIfEmpty(), (card, assignedUserId) => new { card, assignedUserId })
            .GroupJoin(_usersCollection,
                c => c.assignedUserId,
                u => u.Id,
                (x, u) => new { x.card, x.assignedUserId, user = u.FirstOrDefault() })
            .SelectMany(x => x.card.Tags.DefaultIfEmpty(), (x, tagId) => new { x.card, x.assignedUserId, x.user, tagId })
            .Join(_boardsCollection,
                x => x.card.BoardId,
                board => board.Id,
                (x, board) => new { x.card, x.assignedUserId, x.user, x.tagId, board })
            .Select(x => new
            {
                x.card.Id,
                x.card.BoardId,
                x.card.SwimlaneId,
                x.card.ListId,
                x.card.Title,
                x.card.Description,
                x.card.DueDate,
                x.card.CreatedAt,
                x.card.UpdatedAt,
                Tag = x.board.Swimlanes
                    .Where(s => s.Id == x.card.SwimlaneId)
                    .Select(s => s.Tags.FirstOrDefault(t => t.Id == x.tagId))
                    .FirstOrDefault(),
                AssignedUser = x.user == null ? null : new SimplifiedUser
                {
                    Id = x.user.Id,
                    Nickname = x.user.Nickname,
                },
            })
            .GroupBy(x => x.Id)
            .Select(g => new CardWithAssignedUserAndTags
            {
                Id = g.Key,
                BoardId = g.First().BoardId,
                SwimlaneId = g.First().SwimlaneId,
                ListId = g.First().ListId,
                Title = g.First().Title,
                Description = g.First().Description,
                DueDate = g.First().DueDate,
                CreatedAt = g.First().CreatedAt,
                UpdatedAt = g.First().UpdatedAt,
                Tags = g.Where(x => x.Tag != null).Select(x => x.Tag!),
                AssignedUsers = g.Where(x => x.AssignedUser != null).Select(x => x.AssignedUser!),
            }).FirstOrDefaultAsync(cancellationToken: cancellationToken);
        if (data == null)
            return null;
        if (LockedCards.TryGetValue(data.Id, out var lockedUserId))
        {
            var lockedUser = await GetLockingUserById(lockedUserId, session, cancellationToken);
            if (lockedUser != null)
            {
                data.LockedByUser = new SimplifiedUser
                {
                    Id = lockedUser.Id,
                    Nickname = lockedUser.Nickname
                };
            }
        }
        return data;
    }

    public bool LockCard(ObjectId cardId, ObjectId userId) =>
        LockedCards.TryAdd(cardId, userId);

    private IQueryable<SimplifiedUser> GetSimplifiedUser(IClientSessionHandle? session = null)
    {
        return (session == null
            ? _usersCollection.AsQueryable()
            : _usersCollection.AsQueryable(session))
            .Select(x => new SimplifiedUser
            {
                Id = x.Id,
                Nickname = x.Nickname,
            });
    }

    public async Task<SimplifiedUser?> GetLockingUserById(ObjectId id, IClientSessionHandle? session = null, CancellationToken cancellationToken = default)
    {
        return await GetSimplifiedUser(session).FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public void UnlockAllLockedByUser(ObjectId userId)
    {
        var keysToRemove = LockedCards
            .Where(kvp => kvp.Value == userId)
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var key in keysToRemove)
            LockedCards.TryRemove(key, out _);
    }

    public void UnlockCard(ObjectId cardId, ObjectId userId)
    {
        if (LockedCards.TryGetValue(cardId, out var lockedUserId) && lockedUserId == userId)
        {
            LockedCards.TryRemove(cardId, out _);
        }
    }

    public async Task CreateCardAsync(Card card, ObjectId userId, IClientSessionHandle session, CancellationToken cancellationToken = default)
    {
        var result = await _boardsCollection
            .AsQueryable(session)
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
            var filter = Builders<Card>.Filter.Eq(c => c.BoardId, card.BoardId)
                & Builders<Card>.Filter.Eq(c => c.SwimlaneId, card.SwimlaneId)
                & Builders<Card>.Filter.Eq(c => c.ListId, card.ListId);
            long currentWIP = await _cardsCollection.CountDocumentsAsync(session, filter, cancellationToken: cancellationToken);
            if (currentWIP >= result.List.MaxWIP.Value)
                throw new ForbiddenException("Maximum cards WIP limit reached for this list.");
        }
        card.CreatedAt = DateTime.UtcNow;
        card.UpdatedAt = DateTime.UtcNow;
        await _cardsCollection.InsertOneAsync(session, card, cancellationToken: cancellationToken);
    }

    public async Task<CardWithAssignedUserAndTags> CreateAndFindCardAsync(Card card, ObjectId userId, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ct) =>
        {
            await CreateCardAsync(card, userId, s, ct);
            return await GetCardById(card.Id, card.BoardId, s, ct) ?? throw new InvalidOperationException("Card cannot just disappear!!!");
        }, cancellationToken: cancellationToken);
    }

    public async Task UpdateCardAsync(Card card, ObjectId userId, IClientSessionHandle session , CancellationToken cancellationToken = default)
    {
        if (LockedCards.TryGetValue(card.Id, out var lockedUserId) && lockedUserId != userId)
            throw new ForbiddenException("Card is locked by another user. Please try again later.");
        var result = await _boardsCollection
            .AsQueryable(session)
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
            .AsQueryable(session)
            .Where(x => x.Id == card.Id)
            .Select(x => new { x.ListId, x.UpdatedAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (result.MaxWIP.HasValue && result.MaxWIP.Value > 0)
        {
            var countFilter = Builders<Card>.Filter.Eq(c => c.BoardId, card.BoardId)
                & Builders<Card>.Filter.Eq(c => c.SwimlaneId, card.SwimlaneId)
                & Builders<Card>.Filter.Eq(c => c.ListId, card.ListId);
            long currentWIP = await _cardsCollection.CountDocumentsAsync(session, countFilter, cancellationToken: cancellationToken);
            if (oldCard.ListId != card.ListId && currentWIP >= result.MaxWIP.Value)
                throw new ForbiddenException("Maximum WIP limit reached for this list.");
        }

        if (card.UpdatedAt != default && oldCard.UpdatedAt != card.UpdatedAt)
            throw new PreconditionFailedException("Card has been modified or deleted by another user since it was last read.");
        var now = DateTime.UtcNow;
        var update = Builders<Card>.Update
            .Set(c => c.Title, card.Title)
            .Set(c => c.Description, card.Description)
            .Set(c => c.DueDate, card.DueDate)
            .Set(c => c.Tags, card.Tags)
            .Set(c => c.AssignedUsers, card.AssignedUsers)
            .Set(c => c.UpdatedAt, now);
        var filter = Builders<Card>.Filter.Eq(c => c.Id, card.Id);
        if (card.UpdatedAt != default)
            filter &= Builders<Card>.Filter.Eq(c => c.UpdatedAt, card.UpdatedAt);
        UpdateResult updateResult = await _cardsCollection.UpdateOneAsync(session, filter, update, cancellationToken: cancellationToken);
        if (updateResult.MatchedCount == 0 && card.UpdatedAt != default)
            throw new PreconditionFailedException("Card has been modified or deleted by another user by another user since it was last read.");
        else if (updateResult.MatchedCount != 0 && updateResult.ModifiedCount == 0)
            throw new InvalidOperationException("An unexpected error occurred while updating card. Card has not been modified.");
        card.UpdatedAt = now;
        UnlockCard(card.Id, userId);
    }

    public async Task<CardWithAssignedUserAndTags> UpdateAndFindCardAsync(Card card, ObjectId userId, CancellationToken cancellationToken = default)
    {
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        return await session.WithTransactionAsync(async (s, ct) =>
        {
            await UpdateCardAsync(card, userId, s, ct);
            return await GetCardById(card.Id, card.BoardId, s, ct) ?? throw new InvalidOperationException("Card cannot just disappear!!!");
        }, cancellationToken: cancellationToken);
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
        if (LockedCards.TryGetValue(cardId, out var lockedUserId) && lockedUserId != userId)
            throw new ForbiddenException("Card is locked by another user. Please try again later.");
        using var session = await _mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        await session.WithTransactionAsync(async (s, ctx) =>
        {
            var result = await _cardsCollection
            .AsQueryable(s)
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
            .FirstOrDefaultAsync(ctx);
            if (result == null || result.CardId == default)
                throw new RecordDoesNotExist("Card has not been found.");
            if (updatedAt != default && updatedAt != result.UpdatedAt)
                throw new PreconditionFailedException("Card has been modified or deleted by another user since it was last read.");
            if (result.BoardRole == BoardRole.Viewer)
                throw new ForbiddenException("User does not have permission to delete card.");
            var filter = Builders<Card>.Filter.Eq(c => c.Id, cardId);
            if (updatedAt != default)
                filter &= Builders<Card>.Filter.Eq(c => c.UpdatedAt, updatedAt);
            DeleteResult deleteResult = await _cardsCollection.DeleteOneAsync(s, filter, null, cancellationToken: ctx);
            if (deleteResult.DeletedCount == 0 && updatedAt != default)
                throw new PreconditionFailedException("Card has been modified or deleted by another user since it was last read.");
            return Task.CompletedTask;
        }, cancellationToken: cancellationToken);
        UnlockCard(cardId, userId);
    }
}
