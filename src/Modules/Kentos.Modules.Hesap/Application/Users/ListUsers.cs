using Kentos.SharedKernel.Pagination;

namespace Kentos.Modules.Hesap.Application.Users;

/// <summary>Read request — handled directly by the controller via IUserService (no Wolverine).</summary>
public sealed class ListUsersQuery : PagedRequest;
