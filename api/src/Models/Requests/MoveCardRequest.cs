using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace Boardly.Api.Models.Requests;

public class MoveCardRequest
{
    [Required]
    public ObjectId SwimlaneId { get; init; }
}