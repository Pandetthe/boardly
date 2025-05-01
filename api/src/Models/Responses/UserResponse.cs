using Boardly.Api.Entities;

namespace Boardly.Api.Models.Responses;

public record UserResponse(
    string Nickname,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public UserResponse(User user)
        : this(user.Nickname, user.CreatedAt, user.UpdatedAt)
    {
    }
}
