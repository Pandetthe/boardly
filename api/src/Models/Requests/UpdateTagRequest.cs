using Boardly.Api.Entities;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class UpdateTagRequest
{
    [Required, MaxLength(30)]
    public string? Title { get; init; }

    public Color? Color { get; init; }
}