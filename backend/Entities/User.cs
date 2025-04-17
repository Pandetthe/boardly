using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Boardly.Backend.Entities;

public class User
{
    public ObjectId Id { get; set; }

    public string Nickname { get; set; } = null!;

    public string Password { get; set; } = null!;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
