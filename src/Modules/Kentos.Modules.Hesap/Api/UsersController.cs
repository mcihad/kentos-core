using Asp.Versioning;
using Kentos.Modules.Hesap.Application.Users;
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
[Route("api/v{version:apiVersion}/hesap/users")]
public sealed class UsersController(IUserService service, IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(HesapPermissions.User.List)]
    [EndpointSummary("Kullanıcıları listele")]
    public Task<PagedResponse<UserResponse>> List([FromQuery] ListUsersQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    [RequiresPermission(HesapPermissions.User.View)]
    [EndpointSummary("Kullanıcı getir")]
    [ProducesResponseType<UserDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<UserDetailResponse> Get(Guid id, CancellationToken ct) =>
        service.GetByIdAsync(id, ct);

    [HttpPost]
    [RequiresPermission(HesapPermissions.User.Create)]
    [EndpointSummary("Kullanıcı ekle")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<UserResponse>> Create(CreateUserCommand command, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<UserResponse>(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(HesapPermissions.User.Update)]
    [EndpointSummary("Kullanıcı güncelle")]
    public Task<UserResponse> Update(Guid id, UpdateUserRequest request, CancellationToken ct) =>
        bus.InvokeAsync<UserResponse>(new UpdateUserCommand(id, request.Email, request.DisplayName, request.LockoutEnabled), ct);

    [HttpDelete("{id:guid}")]
    [RequiresPermission(HesapPermissions.User.Delete)]
    [EndpointSummary("Kullanıcı sil")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/roles")]
    [RequiresPermission(HesapPermissions.User.AssignRoles)]
    [EndpointSummary("Kullanıcıya rol ata")]
    public Task<UserDetailResponse> AssignRoles(Guid id, AssignUserRolesRequest request, CancellationToken ct) =>
        bus.InvokeAsync<UserDetailResponse>(new AssignUserRolesCommand(id, request.Roles), ct);

    [HttpPut("{id:guid}/departments")]
    [RequiresPermission(HesapPermissions.User.Update)]
    [EndpointSummary("Kullanıcının departmanlarını ayarla")]
    public Task<UserDetailResponse> SetDepartments(Guid id, SetUserDepartmentsRequest request, CancellationToken ct) =>
        bus.InvokeAsync<UserDetailResponse>(new SetUserDepartmentsCommand(id, request.DepartmentIds), ct);
}

/// <summary>Body for updating a user (the id comes from the route).</summary>
public sealed record UpdateUserRequest(string? Email, string? DisplayName, bool LockoutEnabled);

/// <summary>Body for assigning roles to a user (the id comes from the route).</summary>
public sealed record AssignUserRolesRequest(IReadOnlyList<string> Roles);

/// <summary>Body for setting a user's departments (the id comes from the route).</summary>
public sealed record SetUserDepartmentsRequest(IReadOnlyList<Guid> DepartmentIds);
