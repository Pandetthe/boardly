using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

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