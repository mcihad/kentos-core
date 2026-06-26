using Asp.Versioning;
using Kentos.Modules.Hesap.Application.Groups;
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
[Route("api/v{version:apiVersion}/hesap/groups")]
public sealed class GroupsController(IUserGroupService service, IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(HesapPermissions.Group.List)]
    [EndpointSummary("Grupları listele")]
    public Task<PagedResponse<GroupResponse>> List([FromQuery] ListGroupsQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    [RequiresPermission(HesapPermissions.Group.View)]
    [EndpointSummary("Grup getir")]
    [ProducesResponseType<GroupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<GroupResponse> Get(Guid id, CancellationToken ct) =>
        service.GetByIdAsync(id, ct);

    [HttpPost]
    [RequiresPermission(HesapPermissions.Group.Create)]
    [EndpointSummary("Grup ekle")]
    [ProducesResponseType<GroupResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<GroupResponse>> Create(CreateGroupCommand command, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<GroupResponse>(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(HesapPermissions.Group.Update)]
    [EndpointSummary("Grup güncelle")]
    public Task<GroupResponse> Update(Guid id, UpdateGroupRequest request, CancellationToken ct) =>
        bus.InvokeAsync<GroupResponse>(new UpdateGroupCommand(id, request.Name, request.Description), ct);

    [HttpDelete("{id:guid}")]
    [RequiresPermission(HesapPermissions.Group.Delete)]
    [EndpointSummary("Grup sil")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/members")]
    [RequiresPermission(HesapPermissions.Group.View)]
    [EndpointSummary("Grup üyelerini listele")]
    public Task<IReadOnlyList<GroupMemberResponse>> ListMembers(Guid id, CancellationToken ct) =>
        service.ListMembersAsync(id, ct);

    [HttpPost("{id:guid}/members")]
    [RequiresPermission(HesapPermissions.Group.Update)]
    [EndpointSummary("Gruba üye ekle")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddMember(Guid id, GroupMemberRequest request, CancellationToken ct)
    {
        await bus.InvokeAsync(new AddGroupMemberCommand(id, request.UserId), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [RequiresPermission(HesapPermissions.Group.Update)]
    [EndpointSummary("Gruptan üye çıkar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct)
    {
        await service.RemoveMemberAsync(id, userId, ct);
        return NoContent();
    }
}

/// <summary>Body for updating a group (the id comes from the route).</summary>
public sealed record UpdateGroupRequest(string Name, string? Description);

/// <summary>Body for adding a member to a group (the group id comes from the route).</summary>
public sealed record GroupMemberRequest(Guid UserId);
