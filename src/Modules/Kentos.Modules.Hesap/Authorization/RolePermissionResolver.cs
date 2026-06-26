using Kentos.Modules.Hesap.Infrastructure;
using Kentos.SharedKernel.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Kentos.Modules.Hesap.Authorization;

/// <summary>
/// DB-backed <see cref="IPermissionResolver"/>. Holds an in-memory snapshot of the
/// role → permission map (role name → permission keys), rebuilt lazily after
/// invalidation via a volatile reference swap (thread-safe for a read-mostly map).
/// </summary>
public sealed class RolePermissionResolver : IPermissionResolver, IPermissionCacheInvalidator
{
    private readonly IServiceScopeFactory _scopes;
    private volatile IReadOnlyDictionary<string, HashSet<string>>? _snapshot;

    public RolePermissionResolver(IServiceScopeFactory scopes) => _scopes = scopes;

    public IReadOnlySet<string> ResolvePermissions(IEnumerable<string> roles)
    {
        var map = _snapshot ??= Build();
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var role in roles)
        {
            if (map.TryGetValue(role, out var permissions))
            {
                result.UnionWith(permissions);
            }
        }

        return result;
    }

    public void Invalidate() => _snapshot = null;

    private IReadOnlyDictionary<string, HashSet<string>> Build()
    {
        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HesapDbContext>();

        var rows = db.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.Role!.Name != null)
            .Select(rp => new { Role = rp.Role!.Name!, Permission = rp.Permission!.Key })
            .ToList();

        var map = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            if (!map.TryGetValue(row.Role, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                map[row.Role] = set;
            }

            set.Add(row.Permission);
        }

        return map;
    }
}
