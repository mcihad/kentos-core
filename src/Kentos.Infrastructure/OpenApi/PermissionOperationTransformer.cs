using Kentos.SharedKernel.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Kentos.Infrastructure.OpenApi;

/// <summary>
/// Surfaces each endpoint's required permission in OpenAPI as the
/// <c>x-required-permission</c> extension (and in the description), so it appears in
/// generated clients and Scalar.
/// </summary>
public sealed class PermissionOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var attribute = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<RequiresPermissionAttribute>()
            .FirstOrDefault();

        if (attribute is not null)
        {
            operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
            operation.Extensions["x-required-permission"] = new JsonNodeExtension(attribute.Permission);
            operation.Description = $"**Permission:** `{attribute.Permission}`\n\n{operation.Description}";
        }

        return Task.CompletedTask;
    }
}
