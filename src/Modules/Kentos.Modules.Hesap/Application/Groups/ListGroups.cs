using Kentos.SharedKernel.Pagination;

namespace Kentos.Modules.Hesap.Application.Groups;

/// <summary>Read request — handled directly by the controller via IUserGroupService (no Wolverine).</summary>
public sealed class ListGroupsQuery : PagedRequest;
