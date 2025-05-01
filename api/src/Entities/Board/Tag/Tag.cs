using MongoDB.Bson;

namespace Boardly.Api.Entities.Board.Tag;

public class Tag
{
    public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

    public string Title { get; set; } = null!;

    public TagColor? Color { get; set; }

    public override int GetHashCode() => Id.GetHashCode();
}
