using Boardly.Api.Entities.Board;
using Boardly.Api.Entities.Board.Tag;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class CreateRequestTag
{
    [Required]
    public string Title { get; init; } = null!;

    public TagColor? Color { get; init; }
}