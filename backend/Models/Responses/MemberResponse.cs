using Boardly.Backend.Entities;

namespace Boardly.Backend.Models.Responses;

public record MemberResponse(
    string UserId,
    BoardRole Role)
{
    public MemberResponse(Member member)
        : this(member.UserId.ToString(), member.Role)
    {
    }
}