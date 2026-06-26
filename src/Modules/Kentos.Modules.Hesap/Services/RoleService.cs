using Kentos.Modules.Hesap.Application.Roles;
using Kentos.Modules.Hesap.Domain;
using Kentos.Modules.Hesap.Infrastructure;
using Kentos.SharedKernel.Authorization;
using Kentos.SharedKernel.Exceptions;
using Kentos.SharedKernel.Pagination;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.Hesap.Services;

/// <summary>Role data/business operations. Roles are user-managed; permissions are system-defined.</summary>
public interface IRoleService
{
    Task<PagedResponse<RoleResponse>> ListAsync(ListRolesQuery query, CancellationToken cancellationToken);
    Task<RoleDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<RoleResponse> CreateAsync(CreateRoleCommand command, CancellationToken cancellationToken);
    Task<RoleResponse> UpdateAsync(UpdateRoleCommand command, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<RoleDetailResponse> AssignPermissionsAsync(AssignRolePermissionsCommand command, CancellationToken cancellationToken);
}

public sealed class RoleService : IRoleService
{
    private readonly HesapDbContext _db;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly IPermissionCacheInvalidator _cache;

    public RoleService(HesapDbContext db, RoleManager<ApplicationRole> roles, IPermissionCacheInvalidator cache)
    {
        _db = db;
        _roles = roles;
        _cache = cache;
    }

    public async Task<PagedResponse<RoleResponse>> ListAsync(ListRolesQuery query, CancellationToken cancellationToken)
    {
        var source = _db.Set<ApplicationRole>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            source = source.Where(r => EF.Functions.ILike(r.Name!, term));
        }

        var total = await source.LongCountAsync(cancellationToken);
        var items = await source
            .OrderBy(r => r.Name)
            .Skip(query.Skip).Take(query.PageSize)
            .ProjectToType<RoleResponse>()
            .ToListAsync(cancellationToken);

        return new PagedResponse<RoleResponse>(items, total, query.Page, query.PageSize);
    }

    public async Task<RoleDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Set<ApplicationRole>().AsNoTracking()
            .Where(r => r.Uuid == id)
            .ProjectToType<RoleDetailResponse>()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFoundException.For("Rol", id);

    public async Task<RoleResponse> CreateAsync(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        var role = new ApplicationRole { Name = command.Name, Description = command.Description };
        ThrowIfFailed(await _roles.CreateAsync(role));
        return await ProjectResponseAsync(role.Uuid, cancellationToken);
    }

    public async Task<RoleResponse> UpdateAsync(UpdateRoleCommand command, CancellationToken cancellationToken)
    {
        var role = await _roles.Roles.FirstOrDefaultAsync(r => r.Uuid == command.Id, cancellationToken)
            ?? throw NotFoundException.For("Rol", command.Id);

        role.Name = command.Name;
        role.Description = command.Description;
        ThrowIfFailed(await _roles.UpdateAsync(role));

        _cache.Invalidate(); // role name may have changed → role→permission map keys are stale
        return await ProjectResponseAsync(role.Uuid, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await _roles.Roles.FirstOrDefaultAsync(r => r.Uuid == id, cancellationToken)
            ?? throw NotFoundException.For("Rol", id);

        ThrowIfFailed(await _roles.DeleteAsync(role));
        _cache.Invalidate();
    }

    public async Task<RoleDetailResponse> AssignPermissionsAsync(
        AssignRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        var role = await _db.Set<ApplicationRole>().FirstOrDefaultAsync(r => r.Uuid == command.RoleId, cancellationToken)
            ?? throw NotFoundException.For("Rol", command.RoleId);

        var keys = command.PermissionKeys.Distinct(StringComparer.Ordinal).ToList();
        var permissions = await _db.Permissions
            .Where(p => keys.Contains(p.Key))
            .Select(p => new { p.Id, p.Key })
            .ToListAsync(cancellationToken);

        var unknown = keys.Except(permissions.Select(p => p.Key), StringComparer.Ordinal).ToList();
        if (unknown.Count > 0)
        {
            throw new BusinessRuleException($"Bilinmeyen yetki anahtar(lar)ı: {string.Join(", ", unknown)}");
        }

        // Hard-delete old grants (join rows are not soft-deleted, so re-assigning the same
        // permission later cannot collide with the unique index).
        await _db.RolePermissions.Where(rp => rp.RoleId == role.Id).ExecuteDeleteAsync(cancellationToken);
        _db.RolePermissions.AddRange(permissions.Select(p => new RolePermission { RoleId = role.Id, PermissionId = p.Id }));
        await _db.SaveChangesAsync(cancellationToken);

        _cache.Invalidate();
        return await GetByIdAsync(command.RoleId, cancellationToken);
    }

    private async Task<RoleResponse> ProjectResponseAsync(Guid uuid, CancellationToken cancellationToken) =>
        await _db.Set<ApplicationRole>().AsNoTracking()
            .Where(r => r.Uuid == uuid)
            .ProjectToType<RoleResponse>()
            .FirstAsync(cancellationToken);

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new BusinessRuleException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
