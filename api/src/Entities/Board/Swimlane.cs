using MongoDB.Bson;

namespace Boardly.Api.Entities.Board;

public class Swimlane
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public string Title { get; set; } = null!;

    public HashSet<Tag> Tags { get; set; } = [];

    public HashSet<List> Lists { get; set; } = [];

    public Color Color { get; set; }

    public override int GetHashCode() => Id.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is Swimlane other)
        {
            return Id == other.Id;
        }
        return false;
    }
}