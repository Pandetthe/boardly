using System.ComponentModel.DataAnnotations;

namespace Boardly.Backend.Models.Board;

public record CreateRequest
{
    [Required]
    public string Title { get; init; } = null!;

    public string? Description { get; init; }
}
