using MongoDB.Bson;

namespace Boardly.Api.Entities.Board;

public class Swimlane
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public List<Tag> Tags { get; set; } = [];

    public List<List> Lists { get; set; } = [];

    public override int GetHashCode() => Id.GetHashCode();
}