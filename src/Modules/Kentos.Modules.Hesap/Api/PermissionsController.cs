using Asp.Versioning;
using Kentos.Modules.Hesap.Application.Permissions;
using Kentos.Modules.Hesap.Permissions;
using Kentos.Modules.Hesap.Services;
using Kentos.SharedKernel.Authorization;
using Kentos.SharedKernel.Pagination;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kentos.Modules.Hesap.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hesap/permissions")]
public sealed class PermissionsController(IPermissionCatalogService service) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(HesapPermissions.Permission.List)]
    [EndpointSummary("Yetkileri listele")]
    public Task<PagedResponse<PermissionResponse>> List([FromQuery] ListPermissionsQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);
}
