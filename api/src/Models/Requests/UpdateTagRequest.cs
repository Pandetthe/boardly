using Boardly.Api.Entities;

namespace Boardly.Api.Models.Requests;

public class UpdateTagRequest
{
    public string? Title { get; init; }

    public Color? Color { get; init; }
}