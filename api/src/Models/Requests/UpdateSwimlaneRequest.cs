using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class UpdateSwimlaneRequest
{
    [Required]
    public string Title { get; init; } = null!;

    public string? Description { get; init; }
}
