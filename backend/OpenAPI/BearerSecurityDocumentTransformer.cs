using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Boardly.Backend.OpenAPI;

internal sealed class BearerSecurityDocumentTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider,
    IApiDescriptionGroupCollectionProvider apiDescriptionProvider)
    : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
            return;

        OpenApiSecurityScheme bearerScheme = new()
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            In = ParameterLocation.Header,
            BearerFormat = "JWT",
            Reference = new OpenApiReference
            {
                Id = "Bearer",
                Type = ReferenceType.SecurityScheme
            }
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, OpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = bearerScheme;

        IEnumerable<ApiDescription>? apiDescriptions = [.. apiDescriptionProvider.ApiDescriptionGroups
            .Items
            .SelectMany(group => group.Items)];

        foreach (var (pathKey, pathItem) in document.Paths)
        {
            string normalizedPath = pathKey.Trim('/').ToLowerInvariant();
            foreach (var (httpMethod, operation) in pathItem.Operations)
            {
                string method = httpMethod.ToString().ToLowerInvariant();

                ApiDescription? apiDesc = apiDescriptions.FirstOrDefault(desc =>
                    desc.RelativePath?.Split('?')[0]?.Trim('/').ToLowerInvariant() == normalizedPath &&
                    desc.HttpMethod?.ToLowerInvariant() == method);

                if (apiDesc == null)
                    continue;

                bool hasAllowAnonymous = apiDesc.ActionDescriptor.EndpointMetadata
                    .OfType<AllowAnonymousAttribute>()
                    .Any();

                bool hasAuthorize = apiDesc.ActionDescriptor.EndpointMetadata
                    .OfType<AuthorizeAttribute>()
                    .Any();
                if (!hasAllowAnonymous && hasAuthorize)
                {
                    operation.Security ??= [];
                    operation.Security.Add(new OpenApiSecurityRequirement
                    {
                        [bearerScheme] = Array.Empty<string>()
                    });
                }
            }
        }
    }
}
