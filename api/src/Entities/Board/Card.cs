using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Boardly.Api.Entities.Board;

public class Card
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public ObjectId BoardId { get; init; }

    public ObjectId SwimlaneId { get; init; }

    public ObjectId ListId { get; init; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? DueDate { get; set; }

    public HashSet<ObjectId> AssignedUsers { get; set; } = [];

    public HashSet<ObjectId> Tags { get; set; } = [];

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public override int GetHashCode() => Id.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is Card other)
        {
            return Id == other.Id;
        }
        return false;
    }
}