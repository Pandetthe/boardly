using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Dtos;

public class MemberWithUser
{
    public ObjectId UserId { get; set; }

    public string Nickname { get; set; } = null!;

    public BoardRole Role { get; set; }

    public bool IsActive { get; set; }
}
