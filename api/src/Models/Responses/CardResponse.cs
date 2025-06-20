using Boardly.Api.Entities.Board;
using Boardly.Api.Models.Dtos;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

public record CardResponse
(
    ObjectId Id,
    ObjectId BoardId,
    ObjectId SwimlaneId, 
    ObjectId ListId,
    string Title,
    string? Description,
    DateTime? DueDate,
    HashSet<SimplifiedUserResponse>? AssignedUsers,
    SimplifiedUserResponse? LockedByUser,
    HashSet<TagResponse>? Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt
)
{
    public CardResponse(CardWithAssignedUserAndTags card) : this(
        card.Id, card.BoardId, card.SwimlaneId, card.ListId, card.Title,
        card.Description, card.DueDate,
        card.AssignedUsers?.Select(x => new SimplifiedUserResponse(x)).ToHashSet(),
        card.LockedByUser != null ? new SimplifiedUserResponse(card.LockedByUser) : null,
        card.Tags?.Select(x => new TagResponse(x)).ToHashSet(),
        card.CreatedAt, card.UpdatedAt)
    {}
}