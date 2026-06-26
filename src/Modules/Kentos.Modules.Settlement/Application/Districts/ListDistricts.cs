using Kentos.SharedKernel.Pagination;

namespace Kentos.Modules.Settlement.Application.Districts;

/// <summary>Read request — handled directly by the controller via IDistrictService (no Wolverine).</summary>
public sealed class ListDistrictsQuery : PagedRequest
{
    /// <summary>Optional filter by parent province (its public id).</summary>
    public Guid? ProvinceId { get; set; }
}
