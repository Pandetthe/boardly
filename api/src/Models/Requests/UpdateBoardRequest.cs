using Boardly.Api.Entities.Board;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class UpdateBoardRequest
{
    [Required]
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
}