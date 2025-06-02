using Boardly.Api.Entities;
using Boardly.Api.Entities.Board;

namespace Boardly.Api.Models.Responses;

public record TagResponse(
    string Id,
    string Title,
    Color? Color)
{
    public TagResponse(Tag tag) : this(tag.Id.ToString(), tag.Title, tag.Color)
    {

    }
}