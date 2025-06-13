using Boardly.Api.Models.Responses;
using MongoDB.Bson;

public interface IBoardClient
{
    Task Error(string message, CancellationToken cancellationToken = default);

    Task BoardUpdated(BoardResponse board, CancellationToken cancellationToken = default);

    Task BoardDeleted(CancellationToken cancellationToken = default);

    Task SwimlaneCreated(DetailedSwimlaneResponse swimlane, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task SwimlaneUpdated(SwimlaneResponse swimlane, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task SwimlaneDeleted(ObjectId swimlaneId, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task ListCreated(ListResponse list, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task ListUpdated(ListResponse list, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task ListDeleted(ObjectId swimlaneId, ObjectId listId, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task TagCreated(TagResponse tag, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task TagUpdated(TagResponse tag, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task TagDeleted(ObjectId tagId, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task CardEditingStarted(ObjectId cardId, ObjectId userId, CancellationToken cancellationToken = default);

    Task CardEditingFinished(CardResponse card, CancellationToken cancellationToken = default);

    Task CardCreated(CardResponse card, CancellationToken cancellationToken = default);

    Task CardUpdated(CardResponse card, CancellationToken cancellationToken = default);

    Task CardDeleted(ObjectId cardId, CancellationToken cancellationToken = default);

    Task CardMoved(ObjectId cardId, ObjectId swimlaneId, ObjectId listId, DateTime updatedAt, CancellationToken cancellationToken = default);
}