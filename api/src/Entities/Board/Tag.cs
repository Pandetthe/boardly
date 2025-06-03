using MongoDB.Bson;

namespace Boardly.Api.Entities.Board;

public class Tag
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public string Title { get; set; } = null!;

    public Color? Color { get; set; }

    public override int GetHashCode() => Id.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is Tag other)
        {
            return Id == other.Id;
        }
        return false;
    }
}
