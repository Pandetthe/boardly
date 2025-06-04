using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Dtos;

public class MemberWithUser
{
    public ObjectId UserId { get; set; }

    public string Nickname { get; set; } = null!;

    public BoardRole Role { get; set; }

    public bool IsActive { get; set; }

    public override int GetHashCode() => UserId.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is MemberWithUser other)
        {
            return UserId == other.UserId;
        }
        return false;
    }
}
