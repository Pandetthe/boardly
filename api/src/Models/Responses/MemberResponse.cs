using Boardly.Api.Entities.Board;
using Boardly.Api.Models.Dtos;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

public record MemberResponse(
    ObjectId UserId,
    string Nickname,
    BoardRole Role,
    bool IsActive)
{
    public MemberResponse(MemberWithUser member)
        : this(member.UserId, member.Nickname, member.Role, member.IsActive)
    {
    }

    public MemberResponse(Member member)
        : this(member.UserId, "", member.Role, member.IsActive)
    {
    }
}