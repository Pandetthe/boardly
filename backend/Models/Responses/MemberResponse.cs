using Boardly.Backend.Entities;
using MongoDB.Bson;

namespace Boardly.Backend.Models.Responses;

public record MemberResponse(
    ObjectId UserId,
    BoardRole Role)
{
    public MemberResponse(Member member)
        : this(member.UserId, member.Role)
    {
    }
}