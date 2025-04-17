using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Boardly.Backend.Entities;

public class Board
{
    public ObjectId Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    
    public List<Member> Members { get; set; } = [];

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Member
{
    public ObjectId UserId { get; set; }
}
