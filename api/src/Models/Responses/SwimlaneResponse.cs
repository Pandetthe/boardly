using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;
public record SwimlaneResponse(
    ObjectId Id,
    string Title,
    string? Description)
{
    public SwimlaneResponse(Swimlane swimlane) : this(swimlane.Id, swimlane.Title, swimlane.Description)
    {

    }
}