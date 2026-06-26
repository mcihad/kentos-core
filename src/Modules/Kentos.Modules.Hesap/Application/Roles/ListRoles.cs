using Kentos.SharedKernel.Pagination;

namespace Kentos.Modules.Hesap.Application.Roles;

/// <summary>Read request — handled directly by the controller via IRoleService (no Wolverine).</summary>
public sealed class ListRolesQuery : PagedRequest;
