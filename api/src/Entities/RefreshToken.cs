using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Boardly.Api.Entities;

public class RefreshToken
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public ObjectId UserId { get; init; }

    public string Token { get; set; } = null!;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAt { get; set; }
}
