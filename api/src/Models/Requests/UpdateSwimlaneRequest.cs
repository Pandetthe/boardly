namespace Boardly.Api.Models.Requests;

public class UpdateSwimlaneRequest
{
    public string? Title { get; init; }

    public List<UpdateTagRequest>? Tags { get; init; }

    public List<CreateUpdateListRequest>? Lists { get; init; }
}
