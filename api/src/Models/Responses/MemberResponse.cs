using Boardly.Api.Entities.Board;
using Boardly.Api.Models.Dtos;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

public record MemberResponse(
    ObjectId UserId,
    string Nickname,
    BoardRole Role)
{
    public MemberResponse(MemberWithUser member)
        : this(member.UserId, member.Nickname, member.Role)
    {
    }

    public MemberResponse(Member member)
        : this(member.UserId, "", member.Role)
    {
    }
}