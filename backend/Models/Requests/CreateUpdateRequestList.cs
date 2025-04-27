using System.ComponentModel.DataAnnotations;

namespace Boardly.Backend.Models.Requests;

public class CreateUpdateRequestList
{
    [Required]
    public string Title { get; init; } = null!;

    public string? Description { get; init; }

    public int? MaxWIP { get; init; }
}