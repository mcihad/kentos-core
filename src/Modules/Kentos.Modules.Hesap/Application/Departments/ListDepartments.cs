using Kentos.SharedKernel.Pagination;

namespace Kentos.Modules.Hesap.Application.Departments;

/// <summary>Read request — handled directly by the controller via IDepartmentService (no Wolverine).</summary>
public sealed class ListDepartmentsQuery : PagedRequest;
