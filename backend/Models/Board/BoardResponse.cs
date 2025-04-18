namespace Boardly.Backend.Models.Board;

public record BoardResponse
{
    public string Id { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
