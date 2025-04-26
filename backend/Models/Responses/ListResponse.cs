using Boardly.Backend.Entities;

namespace Boardly.Backend.Models.Responses;

public record ListResponse(
    string Id,
    string Title,
    string? Description,
    HashSet<string> Cards,
    int? MaxWIP)
{
    public ListResponse(List list) : this(list.Id.ToString(), list.Title, list.Description,
        [.. list.Cards.Select(x => x.ToString())], list.MaxWIP)
    {

    }
}