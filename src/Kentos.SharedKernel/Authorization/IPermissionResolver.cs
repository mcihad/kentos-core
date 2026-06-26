namespace Kentos.SharedKernel.Authorization;

/// <summary>
/// Resolves the effective permission keys granted by a set of role names.
/// The JWT carries only roles; permissions are resolved server-side from a
/// cached role → permission map (keeping tokens small regardless of how many
/// permissions exist). Implemented by the Hesap module, backed by the database.
/// </summary>
public interface IPermissionResolver
{
    /// <summary>Returns the union of permission keys granted by the given role names.</summary>
    IReadOnlySet<string> ResolvePermissions(IEnumerable<string> roles);
}

/// <summary>
/// Invalidates the cached role → permission map. Application services that change
/// role/permission assignments depend on this abstraction (not the concrete
/// resolver) so they can refresh authorization without restarting.
/// </summary>
public interface IPermissionCacheInvalidator
{
    /// <summary>Drops the cached snapshot; it is rebuilt lazily on the next resolve.</summary>
    void Invalidate();
}
