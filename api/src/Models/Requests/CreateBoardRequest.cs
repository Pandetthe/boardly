﻿using Boardly.Api.Entities.Board;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class CreateBoardRequest
{
    [Required, MaxLength(60)]
    public string Title { get; init; } = null!;

    public HashSet<CreateRequestMember>? Members { get; init; }

    public List<CreateSwimlaneRequest>? Swimlanes { get; init; }
}

public class CreateRequestMember
{
    [Required]
    public ObjectId UserId { get; init; }

    [Required]
    public CreateRequestBoardRole Role { get; init; }

    public override int GetHashCode() => UserId.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (obj is CreateRequestMember other)
        {
            return UserId == other.UserId;
        }
        return false;
    }
}

public enum CreateRequestBoardRole
{
    Admin = BoardRole.Admin,
    Editor = BoardRole.Editor,
    Viewer = BoardRole.Viewer
}
