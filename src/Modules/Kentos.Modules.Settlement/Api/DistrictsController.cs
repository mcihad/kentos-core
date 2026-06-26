using Asp.Versioning;
using Kentos.Modules.Settlement.Application.Districts;
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
[Route("api/v{version:apiVersion}/settlement/districts")]
public sealed class DistrictsController(IDistrictService service, IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(SettlementPermissions.District.List)]
    [EndpointSummary("İlçeleri listele")]
    public Task<PagedResponse<DistrictResponse>> List([FromQuery] ListDistrictsQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    [RequiresPermission(SettlementPermissions.District.View)]
    [EndpointSummary("İlçe getir")]
    [ProducesResponseType<DistrictResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<DistrictResponse> Get(Guid id, CancellationToken ct) =>
        service.GetByIdAsync(id, ct);

    [HttpPost]
    [RequiresPermission(SettlementPermissions.District.Create)]
    [EndpointSummary("İlçe ekle")]
    [ProducesResponseType<DistrictResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DistrictResponse>> Create(CreateDistrictCommand command, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<DistrictResponse>(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id, version = "1.0" }, result);
    }
}
