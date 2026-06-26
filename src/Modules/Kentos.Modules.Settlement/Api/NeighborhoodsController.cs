using Asp.Versioning;
using Kentos.Modules.Settlement.Application.Neighborhoods;
using Kentos.Modules.Settlement.Permissions;
using Kentos.Modules.Settlement.Services;
using Kentos.SharedKernel.Authorization;
using Kentos.SharedKernel.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Kentos.Modules.Settlement.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/settlement/neighborhoods")]
public sealed class NeighborhoodsController(INeighborhoodService service, IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(SettlementPermissions.Neighborhood.List)]
    [EndpointSummary("Mahalleleri listele")]
    public Task<PagedResponse<NeighborhoodResponse>> List([FromQuery] ListNeighborhoodsQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    [RequiresPermission(SettlementPermissions.Neighborhood.View)]
    [EndpointSummary("Mahalle getir")]
    [ProducesResponseType<NeighborhoodResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<NeighborhoodResponse> Get(Guid id, CancellationToken ct) =>
        service.GetByIdAsync(id, ct);

    // Create/Update go through Wolverine (validation pipeline; Create also publishes an event).
    [HttpPost]
    [RequiresPermission(SettlementPermissions.Neighborhood.Create)]
    [EndpointSummary("Mahalle ekle")]
    [ProducesResponseType<NeighborhoodResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<NeighborhoodResponse>> Create(CreateNeighborhoodCommand command, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<NeighborhoodResponse>(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(SettlementPermissions.Neighborhood.Update)]
    [EndpointSummary("Mahalle güncelle")]
    public Task<NeighborhoodResponse> Update(Guid id, UpdateNeighborhoodRequest request, CancellationToken ct) =>
        bus.InvokeAsync<NeighborhoodResponse>(
            new UpdateNeighborhoodCommand(id, request.Name, request.PostalCode, request.Latitude, request.Longitude, request.BoundaryWkt),
            ct);

    // Delete has no validation and no event → straight to the service.
    [HttpDelete("{id:guid}")]
    [RequiresPermission(SettlementPermissions.Neighborhood.Delete)]
    [EndpointSummary("Mahalle sil")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Body for updating a neighborhood (the id comes from the route).</summary>
public sealed record UpdateNeighborhoodRequest(
    string Name,
    string? PostalCode,
    double? Latitude,
    double? Longitude,
    string? BoundaryWkt);
