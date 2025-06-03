using Boardly.Api.Entities;
using Boardly.Api.Entities.Board;
using MongoDB.Bson;

namespace Boardly.Api.Models.Responses;

public record DetailedListResponse(
    ObjectId Id,
    string Title,
    HashSet<string> Cards,
    Color Color,
    int? MaxWIP)
{
    public DetailedListResponse(List list) : this(list.Id, list.Title,
        [.. list.Cards.Select(x => x.ToString())], list.Color, list.MaxWIP)
    {

    }
}