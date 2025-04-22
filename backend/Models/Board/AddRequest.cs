using System.ComponentModel.DataAnnotations;

namespace Boardly.Backend.Models.Board;

public record AddRequest
{
    [Required]
    public string Title { get; init; } = null!;
}
