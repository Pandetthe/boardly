using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Boardly.Api.Entities.Board;

public class Board
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public string Title { get; set; } = null!;
    
    public HashSet<Member> Members { get; set; } = [];

    public HashSet<Swimlane> Swimlanes { get; set; } = [];

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}