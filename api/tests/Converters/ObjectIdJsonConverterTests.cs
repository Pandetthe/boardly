using Boardly.Api.Converters;
using MongoDB.Bson;
using System.Text.Json;

namespace Boardly.Api.Tests.Converters;

public sealed class ObjectIdJsonConverterTests
{
    private readonly JsonSerializerOptions _options;

    public ObjectIdJsonConverterTests()
    {
        _options = new JsonSerializerOptions();
        _options.Converters.Add(new ObjectIdJsonConverter());
    }

    [Fact]
    public void Read_ValidObjectIdString_ReturnsObjectId()
    {
        var objectId = ObjectId.GenerateNewId();
        var json = $@"""{objectId}""";

        var result = JsonSerializer.Deserialize<ObjectId>(json, _options);

        Assert.Equal(objectId, result);
    }

    [Theory]
    [InlineData(@"""abc""")]
    [InlineData(@"""60b8d5e4f6d2b632b5d4e61fa""")]
    [InlineData(@"""60b8d5e4f6d2b632b5d4e61""")]
    public void Read_InvalidObjectIdString_ThrowsJsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ObjectId>(json, _options));
    }

    [Theory]
    [InlineData(@"""""")]
    [InlineData(@"null")]
    public void Read_EmptyOrNull_ThrowsJsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ObjectId>(json, _options));
    }

    [Fact]
    public void Write_ObjectId_WritesString()
    {
        var objectId = ObjectId.GenerateNewId();

        var json = JsonSerializer.Serialize(objectId, _options);

        Assert.Equal($@"""{objectId}""", json);
    }
}
