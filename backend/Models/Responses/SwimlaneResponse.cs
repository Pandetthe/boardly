using Boardly.Backend.Entities;
using MongoDB.Bson;

namespace Boardly.Backend.Models.Responses;
public record SwimlaneResponse(
    ObjectId Id,
    string Title,
    string? Description)
{
    public SwimlaneResponse(Swimlane swimlane) : this(swimlane.Id, swimlane.Title, swimlane.Description)
    {

    }
}