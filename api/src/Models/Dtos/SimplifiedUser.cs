using MongoDB.Bson;

namespace Boardly.Api.Models.Dtos;

public class SimplifiedUser
{
    public ObjectId Id { get; set; }

    public string Nickname { get; set; } = null!;

    public override int GetHashCode() => Id.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is SimplifiedUser other)
        {
            return Id == other.Id;
        }
        return false;
    }
}
