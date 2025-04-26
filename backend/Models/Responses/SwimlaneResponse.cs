using Boardly.Backend.Entities;

namespace Boardly.Backend.Models.Responses;

public record SwimlaneResponse(
    string Id,
    string Title,
    string? Description,
    HashSet<ListResponse> Lists,
    HashSet<TagResponse> Tags)
{
    public SwimlaneResponse(Swimlane swimlane) : this(swimlane.Id.ToString(), swimlane.Title, swimlane.Description,
        [.. swimlane.Lists.Select(x => new ListResponse(x))], [.. swimlane.Tags.Select(x => new TagResponse(x))])
    {

    }
}
