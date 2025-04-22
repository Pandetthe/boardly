using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Boardly.Backend.Entities;

public class Board
{
    public ObjectId Id { get; init; }

    public string Title { get; set; } = null!;
    
    public List<Member> Members { get; set; } = [];

    public List<Swimlane> Swimlanes { get; set; } = [];

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Member
{
    public ObjectId UserId { get; set; }

    public BoardRole Role { get; set; } = BoardRole.Viewer;

    public bool IsActive { get; set; }
}

public enum BoardRole
{
    Owner,
    Admin,
    Editor,
    Viewer
}

public class Swimlane
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public List<Tag> Tags { get; set; } = [];

    public List<List> Lists { get; set; } = [];
}

public class List
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public List<ObjectId> Cards { get; set; } = [];

    public int? MaxWIP { get; set; }
}

public class Tag
{
    public string Title { get; set; } = null!;

    public Color? Color { get; set; }
}

public enum Color
{
    Red,
    Green,
    Blue,
    Purple,
    Yellow,
    Pink,
    Teal
}