using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

public record MemberResponse(
    ObjectId UserId,
    BoardRole Role)
{
    public MemberResponse(Member member)
        : this(member.UserId, member.Role)
    {
    }
}