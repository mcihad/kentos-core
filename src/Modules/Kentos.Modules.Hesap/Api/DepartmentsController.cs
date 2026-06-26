using Asp.Versioning;
using Kentos.Modules.Hesap.Application.Departments;
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
[Route("api/v{version:apiVersion}/hesap/departments")]
public sealed class DepartmentsController(IDepartmentService service, IMessageBus bus) : ControllerBase
{
    [HttpGet]
    [RequiresPermission(HesapPermissions.Department.List)]
    [EndpointSummary("Departmanları listele")]
    public Task<PagedResponse<DepartmentResponse>> List([FromQuery] ListDepartmentsQuery query, CancellationToken ct) =>
        service.ListAsync(query, ct);

    [HttpGet("{id:guid}")]
    [RequiresPermission(HesapPermissions.Department.View)]
    [EndpointSummary("Departman getir")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<DepartmentResponse> Get(Guid id, CancellationToken ct) =>
        service.GetByIdAsync(id, ct);

    [HttpPost]
    [RequiresPermission(HesapPermissions.Department.Create)]
    [EndpointSummary("Departman ekle")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DepartmentResponse>> Create(CreateDepartmentCommand command, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<DepartmentResponse>(command, ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id, version = "1.0" }, result);
    }

    [HttpPut("{id:guid}")]
    [RequiresPermission(HesapPermissions.Department.Update)]
    [EndpointSummary("Departman güncelle")]
    public Task<DepartmentResponse> Update(Guid id, UpdateDepartmentRequest request, CancellationToken ct) =>
        bus.InvokeAsync<DepartmentResponse>(new UpdateDepartmentCommand(id, request.Name, request.ParentId), ct);

    [HttpDelete("{id:guid}")]
    [RequiresPermission(HesapPermissions.Department.Delete)]
    [EndpointSummary("Departman sil")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}

/// <summary>Body for updating a department (the id comes from the route).</summary>
public sealed record UpdateDepartmentRequest(string Name, Guid? ParentId);
