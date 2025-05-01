using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class CreateRequestSwimlane
{
    [Required]
    public string Title { get; init; } = null!;

    public string? Description { get; init; }

    public List<CreateRequestTag>? Tags { get; init; }

    public List<CreateUpdateRequestList>? Lists { get; init; }
}
