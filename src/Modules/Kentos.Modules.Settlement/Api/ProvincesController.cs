using Asp.Versioning;
using Kentos.Modules.Settlement.Application.Provinces;
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
[Route("api/v{version:apiVersion}/settlement/provinces")]
public sealed class ProvincesController(IProvinceService service, IMessageBus bus) : ControllerBase
{
    // Reads go straight to the service (no message bus ceremony).
    [HttpGet]
    [RequiresPermission(SettlementPermissions.Province.List)]
    [EndpointSummary("İlleri listele")]
    public Task<PagedResponse<ProvinceResponse>> List([FromQuery] ListProvincesQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    [RequiresPermission(SettlementPermissions.Province.View)]
    [EndpointSummary("İl getir")]
    [ProducesResponseType<ProvinceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ProvinceResponse> Get(Guid id, CancellationToken ct) =>
        service.GetByIdAsync(id, ct);

    // Writes go through Wolverine: validation pipeline + a published domain event.
    [HttpPost]
    [RequiresPermission(SettlementPermissions.Province.Create)]
    [EndpointSummary("İl ekle")]
    [ProducesResponseType<ProvinceResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProvinceResponse>> Create(CreateProvinceCommand command, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<ProvinceResponse>(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id, version = "1.0" }, result);
    }
}
