﻿using MongoDB.Bson;

namespace Boardly.Api.Entities.Board;

public class List
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public HashSet<ObjectId> Cards { get; set; } = [];

    public int? MaxWIP { get; set; }

    public override int GetHashCode() => Id.GetHashCode();
}