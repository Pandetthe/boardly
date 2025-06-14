using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Dtos;

public class CardWithAssignedUserAndTags
{
    public ObjectId Id { get; set; }

    public ObjectId BoardId { get; set; }

    public ObjectId SwimlaneId { get; set; }
    
    public ObjectId ListId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    
    public DateTime? DueDate { get; set; }

    public IEnumerable<SimplifiedUser> AssignedUsers { get; set; } = [];

    public IEnumerable<Tag> Tags { get; set; } = [];

    public SimplifiedUser? LockedByUser { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}
