using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Boardly.Backend.Entities;

public class User
{
    public ObjectId Id { get; set; }

    public string Nickname { get; set; } = null!;

    public string Password { get; set; } = null!;

    public List<RefreshToken> RefreshTokens { get; set; } = [];
}

public class RefreshToken
{
    public string Token { get; set; } = null!;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime ExpiresAtUtc { get; set; }
}
