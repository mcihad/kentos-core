using Kentos.SharedKernel.Pagination;

namespace Kentos.Modules.Hesap.Application.Policies;

/// <summary>Read request — handled directly by the controller via IAccessPolicyService (no Wolverine).</summary>
public sealed class ListPoliciesQuery : PagedRequest;
