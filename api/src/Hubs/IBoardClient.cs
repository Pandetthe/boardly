using Boardly.Api.Models.Dtos;
using Boardly.Api.Models.Responses;
using MongoDB.Bson;

namespace Boardly.Api.Hubs;

public interface IBoardClient
{
    Task BoardUpdated(BoardResponse board, CancellationToken cancellationToken = default);

    Task BoardDeleted(CancellationToken cancellationToken = default);

    Task SwimlaneCreated(DetailedSwimlaneResponse swimlane, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task SwimlaneUpdated(SwimlaneResponse swimlane, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task SwimlaneDeleted(ObjectId swimlaneId, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task ListCreated(ObjectId swimlaneId, ListResponse list, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task ListUpdated(ObjectId swimlaneId, ListResponse list, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task ListDeleted(ObjectId swimlaneId, ObjectId listId, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task TagCreated(TagResponse tag, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task TagUpdated(TagResponse tag, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task TagDeleted(ObjectId swimlaneId, ObjectId tagId, DateTime updatedAt, CancellationToken cancellationToken = default);

    Task CardLocked(ObjectId cardId, SimplifiedUserResponse user, CancellationToken cancellationToken = default);

    Task CardUnlocked(ObjectId cardId, CancellationToken cancellationToken = default);

    Task CardCreated(CardResponse card, CancellationToken cancellationToken = default);

    Task CardUpdated(CardResponse card, CancellationToken cancellationToken = default);

    Task CardDeleted(ObjectId cardId, CancellationToken cancellationToken = default);

    Task CardMoved(ObjectId cardId, ObjectId swimlaneId, ObjectId listId, DateTime updatedAt, CancellationToken cancellationToken = default);
}