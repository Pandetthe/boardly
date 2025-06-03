using MongoDB.Bson;

namespace Boardly.Api.Entities.Board;

public class Member
{
    public ObjectId UserId { get; set; }

    public BoardRole Role { get; set; } = BoardRole.Viewer;

    public bool IsActive { get; set; }

    public override int GetHashCode() => UserId.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is Member other)
        {
            return UserId == other.UserId;
        }
        return false;
    }
}
