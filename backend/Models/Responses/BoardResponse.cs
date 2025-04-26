using Boardly.Backend.Entities;

namespace Boardly.Backend.Models.Responses;

public record BoardResponse
(
    string Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt
)
{
    public BoardResponse(Board board) : this(board.Id.ToString(), board.Title, board.CreatedAt, board.UpdatedAt)
    {
    }
}

