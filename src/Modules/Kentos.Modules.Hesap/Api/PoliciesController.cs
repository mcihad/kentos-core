using Asp.Versioning;
using Kentos.Modules.Hesap.Application.Policies;
using Kentos.Modules.Hesap.Permissions;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Authorization;
using Kentos.SharedKernel.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Kentos.Modules.Hesap.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hesap/policies")]
public sealed class PoliciesController(IAccessPolicyService service, IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(HesapPermissions.Policy.List)]
    [EndpointSummary("Erişim politikalarını listele")]
    public Task<PagedResponse<PolicyResponse>> List([FromQuery] ListPoliciesQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpPost]
    [RequiresPermission(HesapPermissions.Policy.Create)]
    [EndpointSummary("Erişim politikası ekle")]
    [ProducesResponseType<PolicyResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<PolicyResponse>> Create(CreatePolicyCommand command, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<PolicyResponse>(command, ct);
        return CreatedAtAction(nameof(List), new { version = "1.0" }, result);
    }

    [HttpDelete("{id:guid}")]
    [RequiresPermission(HesapPermissions.Policy.Delete)]
    [EndpointSummary("Erişim politikası sil")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
