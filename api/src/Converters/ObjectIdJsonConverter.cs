using MongoDB.Bson;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Boardly.Api.Converters;

public class ObjectIdJsonConverter : JsonConverter<ObjectId>
{
    public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value) || !ObjectId.TryParse(value, out var objectId))
        {
            throw new JsonException("Invalid ObjectId format.");
        }
        return objectId;
    }

    public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
