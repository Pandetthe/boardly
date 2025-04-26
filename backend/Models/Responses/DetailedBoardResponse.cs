using Boardly.Backend.Entities;

namespace Boardly.Backend.Models.Responses;

public record DetailedBoardResponse
(
    string Id,
    string Title,
    HashSet<SwimlaneResponse> Swimlanes,
    HashSet<MemberResponse> Members,
    DateTime CreatedAt,
    DateTime UpdatedAt
)
{
    public DetailedBoardResponse(Board board) : this(board.Id.ToString(), board.Title,
        [.. board.Swimlanes.Select(x => new SwimlaneResponse(x))], [.. board.Members.Select(x => new MemberResponse(x))]
        , board.CreatedAt, board.UpdatedAt)
    {
    }
}