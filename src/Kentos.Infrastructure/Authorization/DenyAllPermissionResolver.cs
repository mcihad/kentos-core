using Kentos.SharedKernel.Authorization;

namespace Kentos.Infrastructure.Authorization;

/// <summary>
/// Fail-closed default <see cref="IPermissionResolver"/>: grants nothing. Registered
/// by the core so that, if no module provides a real resolver (the Hesap module is
/// absent), every <c>[RequiresPermission]</c> endpoint denies access rather than
/// failing open. The Hesap module overrides this with a DB-backed resolver.
/// </summary>
public sealed class DenyAllPermissionResolver : IPermissionResolver
{
    private static readonly IReadOnlySet<string> Empty = new HashSet<string>();

    public IReadOnlySet<string> ResolvePermissions(IEnumerable<string> roles) => Empty;
}
