using Boardly.Backend.Entities;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Backend.Models.Requests;

public class CreateRequestTag
{
    [Required]
    public string Title { get; init; } = null!;

    public Color? Color { get; init; }
}