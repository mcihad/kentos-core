using Kentos.Modules.Hesap.Application.Departments;
using Kentos.Modules.Hesap.Domain;
using Kentos.Modules.Hesap.Infrastructure;
using Kentos.SharedKernel.Exceptions;
using Kentos.SharedKernel.Pagination;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.Hesap.Services;

/// <summary>Department data operations (self-referencing tree).</summary>
public interface IDepartmentService
{
    Task<PagedResponse<DepartmentResponse>> ListAsync(ListDepartmentsQuery query, CancellationToken cancellationToken);
    Task<DepartmentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<DepartmentResponse> CreateAsync(CreateDepartmentCommand command, CancellationToken cancellationToken);
    Task<DepartmentResponse> UpdateAsync(UpdateDepartmentCommand command, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class DepartmentService : IDepartmentService
{
    private readonly HesapDbContext _db;

    public DepartmentService(HesapDbContext db) => _db = db;

    public async Task<PagedResponse<DepartmentResponse>> ListAsync(ListDepartmentsQuery query, CancellationToken cancellationToken)
    {
        var source = _db.Departments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            source = source.Where(d => EF.Functions.ILike(d.Name, term));
        }

        var total = await source.LongCountAsync(cancellationToken);
        var items = await source
            .OrderBy(d => d.Name)
            .Skip(query.Skip).Take(query.PageSize)
            .ProjectToType<DepartmentResponse>()
            .ToListAsync(cancellationToken);

        return new PagedResponse<DepartmentResponse>(items, total, query.Page, query.PageSize);
    }

    public async Task<DepartmentResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.Departments.AsNoTracking()
            .Where(d => d.Uuid == id)
            .ProjectToType<DepartmentResponse>()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFoundException.For("Departman", id);

    public async Task<DepartmentResponse> CreateAsync(CreateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var department = new Department
        {
            Name = command.Name,
            ParentId = await ResolveParentAsync(command.ParentId, cancellationToken),
        };

        _db.Departments.Add(department);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(department.Uuid, cancellationToken);
    }

    public async Task<DepartmentResponse> UpdateAsync(UpdateDepartmentCommand command, CancellationToken cancellationToken)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Uuid == command.Id, cancellationToken)
            ?? throw NotFoundException.For("Departman", command.Id);

        var parentId = await ResolveParentAsync(command.ParentId, cancellationToken);
        if (parentId is not null)
        {
            await EnsureNotCyclicAsync(department.Id, parentId.Value, cancellationToken);
        }

        department.Name = command.Name;
        department.ParentId = parentId;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(command.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var department = await _db.Departments.FirstOrDefaultAsync(d => d.Uuid == id, cancellationToken)
            ?? throw NotFoundException.For("Departman", id);

        if (await _db.Departments.AnyAsync(d => d.ParentId == department.Id, cancellationToken))
        {
            throw new BusinessRuleException("Alt departmanları olan bir departman silinemez.");
        }

        _db.Departments.Remove(department);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<long?> ResolveParentAsync(Guid? parentUuid, CancellationToken cancellationToken)
    {
        if (parentUuid is null)
        {
            return null;
        }

        var parent = await _db.Departments
            .Where(d => d.Uuid == parentUuid.Value)
            .Select(d => (long?)d.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return parent ?? throw NotFoundException.For("Üst departman", parentUuid.Value);
    }

    private async Task EnsureNotCyclicAsync(long departmentId, long parentId, CancellationToken cancellationToken)
    {
        var current = (long?)parentId;
        while (current is not null)
        {
            if (current == departmentId)
            {
                throw new BusinessRuleException("Departman kendi alt ağacına taşınamaz.");
            }

            current = await _db.Departments
                .Where(d => d.Id == current.Value)
                .Select(d => d.ParentId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
