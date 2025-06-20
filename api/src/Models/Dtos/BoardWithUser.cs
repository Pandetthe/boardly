﻿using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Dtos;

public class BoardWithUser
{
    public ObjectId Id { get; set; }

    public string Title { get; set; } = null!;

    public IEnumerable<MemberWithUser> Members { get; set; } = [];

    public IEnumerable<Swimlane> Swimlanes { get; set; } = [];

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
