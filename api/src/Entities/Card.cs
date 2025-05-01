using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Boardly.Api.Entities;

public class Card
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public ObjectId BoardId { get; init; }

    public ObjectId SwimlaneId { get; init; }

    public ObjectId ListId { get; init; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc, DateOnly = true)]
    public DateOnly? DueDate { get; set; }

    public List<ObjectId> AssignedUsers { get; set; } = [];

    public List<ObjectId> Tags { get; set; } = [];

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}