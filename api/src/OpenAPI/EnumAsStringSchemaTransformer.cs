using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Boardly.Api.OpenAPI;

internal class EnumAsStringSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var type = context.JsonTypeInfo.Type;

        if (type.IsEnum)
        {
            var enumNames = Enum.GetNames(type);

            schema.Type = "string";
            schema.Enum.Clear();
            foreach (var name in enumNames)
            {
                schema.Enum.Add(new OpenApiString(char.ToLowerInvariant(name[0]) + name[1..]));
            }
        }
        return Task.CompletedTask;
    }
}
