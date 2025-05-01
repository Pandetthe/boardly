using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

public record DetailedListResponse(
    ObjectId Id,
    string Title,
    string? Description,
    HashSet<string> Cards,
    int? MaxWIP)
{
    public DetailedListResponse(List list) : this(list.Id, list.Title, list.Description,
        [.. list.Cards.Select(x => x.ToString())], list.MaxWIP)
    {

    }
}