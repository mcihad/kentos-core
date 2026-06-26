using Microsoft.AspNetCore.Authorization;

namespace Kentos.SharedKernel.Authorization;

/// <summary>
/// Declares the permission key an endpoint requires. Drives the authorization policy
/// (dynamic policy provider) and surfaces in OpenAPI as <c>x-required-permission</c>
/// so it appears in generated clients.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequiresPermissionAttribute : AuthorizeAttribute
{
    /// <summary>Prefix for the dynamically generated policy names.</summary>
    public const string PolicyPrefix = "perm:";

    public RequiresPermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = PolicyPrefix + permission;
    }

    /// <summary>The required permission key, e.g. "settlement.neighborhood.create".</summary>
    public string Permission { get; }
}
