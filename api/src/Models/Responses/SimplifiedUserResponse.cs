using Boardly.Api.Models.Dtos;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

public record SimplifiedUserResponse(
    ObjectId Id,
    string Nickname)
{
    public SimplifiedUserResponse(SimplifiedUser user)
        : this(user.Id, user.Nickname)
    {
    }
}