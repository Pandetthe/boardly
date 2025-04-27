using System.ComponentModel.DataAnnotations;

namespace Boardly.Backend.Models.Requests;

public class UpdateSwimlaneRequest
{
    [Required]
    public string Title { get; init; } = null!;

    public string? Description { get; init; }
}
