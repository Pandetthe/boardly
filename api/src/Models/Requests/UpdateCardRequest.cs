using MongoDB.Bson;

namespace Boardly.Api.Models.Requests;

public class UpdateCardRequest
{
    public string Title { get; init; } = null!;

    public HashSet<ObjectId>? Tags { get; init; }

    public DateTime? DueDate { get; init; }

    public HashSet<ObjectId>? AssignedUsers { get; init; }

    public string? Description { get; init; }
}