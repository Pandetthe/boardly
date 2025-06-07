using MongoDB.Bson;
using System.Security.Claims;

namespace Boardly.Api.Extensions;

public static class ClaimsPrincipalEx
{
    public static ObjectId GetUserId(this ClaimsPrincipal user)
    {
        string? userIdString = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !ObjectId.TryParse(userIdString, out ObjectId userId))
        {
            throw new InvalidOperationException("User ID is not valid.");
        }
        return userId;
    }
}
