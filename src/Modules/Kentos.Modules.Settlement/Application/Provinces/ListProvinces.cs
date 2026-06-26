using Kentos.SharedKernel.Pagination;

namespace Kentos.Modules.Settlement.Application.Provinces;

/// <summary>Read request — handled directly by the controller via IProvinceService (no Wolverine).</summary>
public sealed class ListProvincesQuery : PagedRequest;
