using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

public record DetailedBoardResponse
(
    ObjectId Id,
    string Title,
    HashSet<DetailedSwimlaneResponse> Swimlanes,
    HashSet<MemberResponse> Members,
    DateTime CreatedAt,
    DateTime UpdatedAt
)
{
    public DetailedBoardResponse(Board board) : this(board.Id, board.Title,
        [.. board.Swimlanes.Select(x => new DetailedSwimlaneResponse(x))], [.. board.Members.Select(x => new MemberResponse(x))]
        , board.CreatedAt, board.UpdatedAt)
    {
    }
}