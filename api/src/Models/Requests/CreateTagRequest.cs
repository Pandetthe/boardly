using Boardly.Api.Entities;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class CreateTagRequest
{
    [Required]
    public string Title { get; init; } = null!;

    [Required]
    public Color Color { get; init; }
}