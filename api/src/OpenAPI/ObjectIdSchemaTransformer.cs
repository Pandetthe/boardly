using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;

namespace Boardly.Api.OpenAPI;

internal class ObjectIdSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.JsonTypeInfo.Type == typeof(ObjectId))
        {
            schema.Type = "string";
            schema.Format = "objectid";
            schema.Example = new OpenApiString("507f1f77bcf86cd799439011");
            schema.Annotations["x-schema-id"] = "ObjectId";
            schema.Title = "ObjectId";
        }

        return Task.CompletedTask;
    }
}