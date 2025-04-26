using Boardly.Backend.Entities;

namespace Boardly.Backend.Models.Responses;

public record TagResponse(
    string Id,
    string Title,
    Color? Color)
{
    public TagResponse(Tag tag) : this(tag.Id.ToString(), tag.Title, tag.Color)
    {

    }
}