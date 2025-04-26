using Boardly.Backend.Entities;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Backend.Models.Requests;

public class CreateRequest
{
    [Required]
    public string Title { get; init; } = null!;

    public HashSet<CreateRequestMember>? Members { get; init; }

    public List<CreateRequestSwimlane>? Swimlanes { get; init; }
}

public class CreateRequestMember
{
    [Required]
    public ObjectId UserId { get; init; }

    [Required]
    public CreateRequestBoardRole Role { get; init; }

    public override int GetHashCode() => UserId.GetHashCode();
}

public enum CreateRequestBoardRole
{
    Admin = BoardRole.Admin,
    Editor = BoardRole.Editor,
    Viewer = BoardRole.Viewer
}

public class CreateRequestSwimlane
{
    [Required]
    public string Title { get; init; } = null!;

    public string? Description { get; init; }

    public List<CreateRequestTag>? Tags { get; init; }

    public List<CreateRequestList>? Lists { get; init; }
}

public class CreateRequestList
{
    [Required]
    public string Title { get; init; } = null!;

    public string? Description { get; init; }

    public int? MaxWIP { get; init; }
}

public class CreateRequestTag
{
    [Required]
    public string Title { get; init; } = null!;

    public Color? Color { get; init; }
}
