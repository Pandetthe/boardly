using Boardly.Api.Entities.Board;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class UpdateBoardRequest
{
    [Required, MaxLength(60)]
    public string Title { get; init; } = null!;

    [Required]
    public HashSet<UpdateRequestMember> Members { get; init; } = [];
}

public class UpdateRequestMember
{
    [Required]
    public ObjectId UserId { get; init; }

    [Required]
    public BoardRole Role { get; init; }

    public override int GetHashCode() => UserId.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is UpdateRequestMember other)
        {
            return UserId == other.UserId;
        }
        return false;
    }
}