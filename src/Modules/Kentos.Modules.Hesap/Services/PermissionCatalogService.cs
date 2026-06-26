using Kentos.Modules.Hesap.Application.Permissions;
using Kentos.Modules.Hesap.Infrastructure;
using Kentos.SharedKernel.Pagination;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.Hesap.Services;

/// <summary>Read-only view of the system-defined permission catalog.</summary>
public interface IPermissionCatalogService
{
    Task<PagedResponse<PermissionResponse>> ListAsync(ListPermissionsQuery query, CancellationToken cancellationToken);
}

public sealed class PermissionCatalogService : IPermissionCatalogService
{
    private readonly HesapDbContext _db;

    public PermissionCatalogService(HesapDbContext db) => _db = db;

    public async Task<PagedResponse<PermissionResponse>> ListAsync(ListPermissionsQuery query, CancellationToken cancellationToken)
    {
        var source = _db.Permissions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            source = source.Where(p => EF.Functions.ILike(p.Key, term) || EF.Functions.ILike(p.Title, term));
        }

        var total = await source.LongCountAsync(cancellationToken);
        var items = await source
            .OrderBy(p => p.Key)
            .Skip(query.Skip).Take(query.PageSize)
            .ProjectToType<PermissionResponse>()
            .ToListAsync(cancellationToken);

        return new PagedResponse<PermissionResponse>(items, total, query.Page, query.PageSize);
    }
}
