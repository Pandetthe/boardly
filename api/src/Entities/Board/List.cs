using MongoDB.Bson;

namespace Boardly.Api.Entities.Board;

public class List
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public string Title { get; set; } = null!;

    public Color Color { get; set; }

    public int? MaxWIP { get; set; }

    public override int GetHashCode() => Id.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is List other)
        {
            return Id == other.Id;
        }
        return false;
    }
}