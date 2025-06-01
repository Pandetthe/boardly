using Boardly.Api.Models.Dtos;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

public record BoardResponse
(
    ObjectId Id,
    string Title,
    HashSet<MemberResponse> Members,
    DateTime CreatedAt,
    DateTime UpdatedAt
)
{
    public BoardResponse(BoardWithUser board) : this(board.Id, board.Title, [.. board.Members.Select(x => new MemberResponse(x))], board.CreatedAt, board.UpdatedAt) { }
}