using Boardly.Api.Entities.Board;
using Boardly.Api.Exceptions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Boardly.Api.Services;

public class CardService(MongoDbProvider mongoDbProvider, ILogger<BoardService> logger)
{
    private readonly IMongoCollection<Card> _cardsCollection = mongoDbProvider.GetCardsCollection();
    private readonly IMongoCollection<Board> _boardsCollection = mongoDbProvider.GetBoardsCollection();
    private readonly ILogger<BoardService> _logger = logger;

    public async Task<Card?> GetCardByIdAsync(ObjectId cardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        Card card = await _cardsCollection.Find(x => x.Id == cardId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Card has not been found.");
        Board board = await _boardsCollection.Find(x => x.Id == card.BoardId).FirstOrDefaultAsync(cancellationToken)
            ?? throw new RecordDoesNotExist("Board has not been found.");
        if (board.Members.Any(x => x.UserId == userId))
            throw new ForbidenException("User is not a member of this board.");
        return card;
    }

    public async Task CreateCardAsync(ObjectId userId, Card card, CancellationToken cancellationToken = default)
    {
        Board board = await _boardsCollection.Find(x => x.Id == card.BoardId).FirstOrDefaultAsync(cancellationToken) 
             ?? throw new RecordDoesNotExist("Board has not been found.");
        Member member = board.Members.FirstOrDefault(x => x.UserId == userId) 
             ?? throw new ForbidenException("User is not a member of this board.");
        if (member.Role == BoardRole.Viewer)
            throw new ForbidenException("User does not have permission to create card.");
        await _cardsCollection.InsertOneAsync(card, cancellationToken: cancellationToken);
    }

    public async Task UpdateCardAsync(ObjectId cardId, ObjectId userId, Card card, CancellationToken cancellationToken = default)
    {
        Board board = await _boardsCollection.Find(x => x.Id == card.BoardId).FirstOrDefaultAsync(cancellationToken) 
                      ?? throw new RecordDoesNotExist("Board has not been found.");
        Member member = board.Members.FirstOrDefault(x => x.UserId == userId) 
                        ?? throw new ForbidenException("User is not a member of this board.");
        if (member.Role == BoardRole.Viewer)
        {
            throw new ForbidenException("User does not have permission to update card.");
        }
        
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

    public async Task MoveCardAsync(ObjectId userId, ObjectId cardId, ObjectId swimlaneId,
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
            .Set(c => c.SwimlaneId, swimlaneId);
        await _cardsCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    public async Task DeleteCardAsync(ObjectId cardId, ObjectId userId, CancellationToken cancellationToken = default)
    {
        Card card = await _cardsCollection.Find(x => x.Id == cardId).FirstOrDefaultAsync(cancellationToken)
                    ?? throw new RecordDoesNotExist("Card has not been found.");
        Board board = await _boardsCollection.Find(x => x.Id == card.BoardId).FirstOrDefaultAsync(cancellationToken) 
                      ?? throw new RecordDoesNotExist("Board has not been found.");
        Member member = board.Members.FirstOrDefault(x => x.UserId == userId) 
                        ?? throw new ForbidenException("User is not a member of this board.");
        if (member.Role == BoardRole.Viewer)
            throw new ForbidenException("User does not have permission to delete card.");
        await _cardsCollection.DeleteOneAsync(x => x.Id == cardId, null, cancellationToken: cancellationToken);
    }
}
