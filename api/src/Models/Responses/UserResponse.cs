using Boardly.Api.Entities;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

public record UserResponse(
    ObjectId Id,
    string Nickname,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public UserResponse(User user)
        : this(user.Id, user.Nickname, user.CreatedAt, user.UpdatedAt)
    {
    }
}
