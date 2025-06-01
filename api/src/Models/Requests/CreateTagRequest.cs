using Boardly.Api.Entities;
using Boardly.Api.Entities.Board;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class CreateTagRequest
{
    [Required]
    public string Title { get; init; } = null!;

    public Color? Color { get; init; }
}