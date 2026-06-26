using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Kentos.Infrastructure.OpenApi;

/// <summary>Adds an HTTP Bearer (JWT) security scheme to the document, so Scalar shows
/// an "Authorize → paste token" box for the self-issued access tokens.</summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Kentos self-issued JWT access token (Bearer).",
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = scheme;

        return Task.CompletedTask;
    }
}
