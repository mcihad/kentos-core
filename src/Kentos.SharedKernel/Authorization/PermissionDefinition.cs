namespace Kentos.SharedKernel.Authorization;

/// <summary>
/// Full definition of a permission. <see cref="Key"/> is <c>module.resource.action</c>.
/// In Keycloak each definition maps to a client role.
/// </summary>
public sealed record PermissionDefinition(
    string Key,
    string Module,
    string Resource,
    string Action,
    string Title,
    string? Description = null)
{
    /// <summary>Builds a definition from its parts, deriving the key.</summary>
    public static PermissionDefinition Create(
        string module,
        string resource,
        string action,
        string title,
        string? description = null)
        => new($"{module}.{resource}.{action}", module, resource, action, title, description);
}
