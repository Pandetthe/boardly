using Boardly.Api.Entities;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class CreateSwimlaneRequest
{
    [Required]
    public string Title { get; init; } = null!;

    public List<CreateTagRequest>? Tags { get; init; }

    public List<CreateUpdateListRequest>? Lists { get; init; }

    [Required]
    public Color Color { get; init; }
}
