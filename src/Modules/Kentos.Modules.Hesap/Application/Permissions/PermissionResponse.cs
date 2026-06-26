using Kentos.SharedKernel.Pagination;

namespace Kentos.Modules.Hesap.Application.Permissions;

public sealed record PermissionResponse(
    Guid Id,
    string Key,
    string Module,
    string Resource,
    string Action,
    string Title,
    string? Description);

/// <summary>Read request — handled directly by the controller via IPermissionCatalogService.</summary>
public sealed class ListPermissionsQuery : PagedRequest;
