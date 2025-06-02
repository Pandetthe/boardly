using Boardly.Api.Entities.Board;
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
    HashSet<MemberResponse>? AssignedUsers,
    HashSet<TagResponse>? Tags,
    DateTime CreatedAt,
    DateTime UpdatedAt
)
{
    public CardResponse(Card card, Board board, Swimlane swimlane) : this(card.Id, card.BoardId, card.SwimlaneId, card.ListId, card.Title,
        card.Description, card.DueDate, 
        board.Members.Where(x => card.AssignedUsers.Contains(x.UserId)).Select(x => new MemberResponse(x)).ToHashSet(),
        swimlane.Tags.Where(x => card.Tags.Contains(x.Id)).Select(x => new TagResponse(x)).ToHashSet(),
        card.CreatedAt, card.UpdatedAt)
    {}
}