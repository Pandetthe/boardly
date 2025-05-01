using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;
public record DetailedSwimlaneResponse(
    ObjectId Id,
    string Title,
    string? Description,
    HashSet<ListResponse> Lists,
    HashSet<TagResponse> Tags)
{
    public DetailedSwimlaneResponse(Swimlane swimlane) : this(swimlane.Id, swimlane.Title, swimlane.Description,
        [.. swimlane.Lists.Select(x => new ListResponse(x))], [.. swimlane.Tags.Select(x => new TagResponse(x))])
    {

    }
}
