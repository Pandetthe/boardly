using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

public record BoardResponse
(
    ObjectId Id,
    string Title,
    DateTime CreatedAt,
    DateTime UpdatedAt
)
{
    public BoardResponse(Board board) : this(board.Id, board.Title, board.CreatedAt, board.UpdatedAt) { }
}