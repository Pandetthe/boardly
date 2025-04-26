using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Boardly.Backend.OpenAPI;

internal sealed class BaseInfoDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Missing";
        string description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "Missing";
        document.Info = new()
        {
            Title = title,
            Version = "0.1.0",
            Description = description
        };
        return Task.CompletedTask;
    }
}
