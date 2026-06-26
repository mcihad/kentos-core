using Asp.Versioning;
using Kentos.Modules.Hesap.Application.Roles;
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
[Route("api/v{version:apiVersion}/hesap/roles")]
public sealed class RolesController(IRoleService service, IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(HesapPermissions.Role.List)]
    [EndpointSummary("Rolleri listele")]
    public Task<PagedResponse<RoleResponse>> List([FromQuery] ListRolesQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    [RequiresPermission(HesapPermissions.Role.View)]
    [EndpointSummary("Rol getir")]
    [ProducesResponseType<RoleDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<RoleDetailResponse> Get(Guid id, CancellationToken ct) =>
        service.GetByIdAsync(id, ct);

    [HttpPost]
    [RequiresPermission(HesapPermissions.Role.Create)]
    [EndpointSummary("Rol ekle")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<RoleResponse>> Create(CreateRoleCommand command, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<RoleResponse>(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(HesapPermissions.Role.Update)]
    [EndpointSummary("Rol güncelle")]
    public Task<RoleResponse> Update(Guid id, UpdateRoleRequest request, CancellationToken ct) =>
        bus.InvokeAsync<RoleResponse>(new UpdateRoleCommand(id, request.Name, request.Description), ct);

    [HttpDelete("{id:guid}")]
    [RequiresPermission(HesapPermissions.Role.Delete)]
    [EndpointSummary("Rol sil")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/permissions")]
    [RequiresPermission(HesapPermissions.Role.AssignPermissions)]
    [EndpointSummary("Role yetki ata")]
    public Task<RoleDetailResponse> AssignPermissions(Guid id, AssignRolePermissionsRequest request, CancellationToken ct) =>
        bus.InvokeAsync<RoleDetailResponse>(new AssignRolePermissionsCommand(id, request.PermissionKeys), ct);
}

/// <summary>Body for updating a role (the id comes from the route).</summary>
public sealed record UpdateRoleRequest(string Name, string? Description);

/// <summary>Body for assigning permissions to a role (the id comes from the route).</summary>
public sealed record AssignRolePermissionsRequest(IReadOnlyList<string> PermissionKeys);
