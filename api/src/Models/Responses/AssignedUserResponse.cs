using Boardly.Api.Models.Dtos;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

public record AssignedUserResponse(
    ObjectId Id,
    string Nickname)
{
    public AssignedUserResponse(AssignedUser user)
        : this(user.Id, user.Nickname)
    {
    }
}