using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class CreateUpdateCardRequest
{
    [Required]
    public string Title { get; init; } = null!;
    
    [Required]
    public ObjectId SwimlaneId { get; init; }
    
    [Required]
    public ObjectId ListId { get; init; }

    public HashSet<ObjectId>? Tags { get; init; }

    public DateTime? DueDate { get; init; }

    public HashSet<ObjectId>? AssignedUsers { get; init; }

    public string? Description { get; init; }
}
