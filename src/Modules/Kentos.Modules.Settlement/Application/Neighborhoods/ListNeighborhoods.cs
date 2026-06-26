using Kentos.SharedKernel.Pagination;

namespace Kentos.Modules.Settlement.Application.Neighborhoods;

/// <summary>Read request — handled directly by the controller via INeighborhoodService (no Wolverine).</summary>
public sealed class ListNeighborhoodsQuery : PagedRequest
{
    /// <summary>Optional filter by parent district (its public id).</summary>
    public Guid? DistrictId { get; set; }
}
