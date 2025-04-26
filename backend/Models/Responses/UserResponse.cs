using Boardly.Backend.Entities;

namespace Boardly.Backend.Models.Responses;

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
