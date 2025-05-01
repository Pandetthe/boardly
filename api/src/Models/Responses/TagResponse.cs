using Boardly.Api.Entities.Board.Tag;

namespace Boardly.Api.Models.Responses;

public record TagResponse(
    string Id,
    string Title,
    TagColor? Color)
{
    public TagResponse(Tag tag) : this(tag.Id.ToString(), tag.Title, tag.Color)
    {

    }
}