using Boardly.Api.Entities;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class UpdateSwimlaneRequest
{
    [Required, MaxLength(20)]
    public string Title { get; init; } = null!;

    [Required]
    public Color Color { get; init; }
}
