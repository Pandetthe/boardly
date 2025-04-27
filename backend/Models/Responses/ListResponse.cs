using Boardly.Backend.Entities;
using MongoDB.Bson;

namespace Boardly.Backend.Models.Responses;

public record ListResponse(
    ObjectId Id,
    string Title,
    string? Description,
    int? MaxWIP)
{
    public ListResponse(List list) : this(list.Id, list.Title, list.Description, list.MaxWIP)
    {

    }
}