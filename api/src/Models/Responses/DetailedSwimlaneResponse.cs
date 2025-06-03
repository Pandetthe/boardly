using Boardly.Api.Entities;
using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;
public record DetailedSwimlaneResponse(
    ObjectId Id,
    string Title,
    HashSet<ListResponse> Lists,
    HashSet<TagResponse> Tags,
    Color Color)
{
    public DetailedSwimlaneResponse(Swimlane swimlane) : this(swimlane.Id, swimlane.Title,
        [.. swimlane.Lists.Select(x => new ListResponse(x))], [.. swimlane.Tags.Select(x => new TagResponse(x))],
        swimlane.Color)
    {

    }
}
