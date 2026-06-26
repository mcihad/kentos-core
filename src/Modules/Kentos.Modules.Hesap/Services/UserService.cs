using Kentos.Modules.Hesap.Application.Users;
using Kentos.Modules.Hesap.Domain;
using Kentos.Modules.Hesap.Infrastructure;
using Kentos.SharedKernel.Exceptions;
using Kentos.SharedKernel.Pagination;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.Hesap.Services;

/// <summary>User data/business operations (ASP.NET Identity + departments).</summary>
public interface IUserService
{
    Task<PagedResponse<UserResponse>> ListAsync(ListUsersQuery query, CancellationToken cancellationToken);
    Task<UserDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserResponse> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken);
    Task<UserResponse> UpdateAsync(UpdateUserCommand command, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<UserDetailResponse> AssignRolesAsync(AssignUserRolesCommand command, CancellationToken cancellationToken);
    Task<UserDetailResponse> SetDepartmentsAsync(SetUserDepartmentsCommand command, CancellationToken cancellationToken);
}

public sealed class UserService : IUserService
{
    private readonly HesapDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IMapper _mapper;

    public UserService(HesapDbContext db, UserManager<ApplicationUser> users, IMapper mapper)
    {
        _db = db;
        _users = users;
        _mapper = mapper;
    }

    public async Task<PagedResponse<UserResponse>> ListAsync(ListUsersQuery query, CancellationToken cancellationToken)
    {
        var source = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            source = source.Where(u => EF.Functions.ILike(u.UserName!, term) || EF.Functions.ILike(u.Email!, term));
        }

        var total = await source.LongCountAsync(cancellationToken);
        var items = await source
            .OrderBy(u => u.UserName)
            .Skip(query.Skip).Take(query.PageSize)
            .ProjectToType<UserResponse>()
            .ToListAsync(cancellationToken);

        return new PagedResponse<UserResponse>(items, total, query.Page, query.PageSize);
    }

    public async Task<UserDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Uuid == id, cancellationToken)
            ?? throw NotFoundException.For("Kullanıcı", id);

        return await ToDetailAsync(user, cancellationToken);
    }

    public async Task<UserResponse> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            UserName = command.UserName,
            Email = command.Email,
            EmailConfirmed = true,
            DisplayName = command.DisplayName,
            LockoutEnabled = true,
        };

        ThrowIfFailed(await _users.CreateAsync(user, command.Password));

        if (command.Roles is { Count: > 0 })
        {
            await AssignRolesInternalAsync(user, command.Roles);
        }

        return _mapper.Map<UserResponse>(user);
    }

    public async Task<UserResponse> UpdateAsync(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.Uuid == command.Id, cancellationToken)
            ?? throw NotFoundException.For("Kullanıcı", command.Id);

        if (command.Email is not null)
        {
            user.Email = command.Email;
        }

        user.DisplayName = command.DisplayName;
        user.LockoutEnabled = command.LockoutEnabled;

        ThrowIfFailed(await _users.UpdateAsync(user));
        return _mapper.Map<UserResponse>(user);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.Uuid == id, cancellationToken)
            ?? throw NotFoundException.For("Kullanıcı", id);

        ThrowIfFailed(await _users.DeleteAsync(user));
    }

    public async Task<UserDetailResponse> AssignRolesAsync(AssignUserRolesCommand command, CancellationToken cancellationToken)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.Uuid == command.UserId, cancellationToken)
            ?? throw NotFoundException.For("Kullanıcı", command.UserId);

        await AssignRolesInternalAsync(user, command.Roles);
        return await ToDetailAsync(user, cancellationToken);
    }

    public async Task<UserDetailResponse> SetDepartmentsAsync(SetUserDepartmentsCommand command, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Uuid == command.UserId, cancellationToken)
            ?? throw NotFoundException.For("Kullanıcı", command.UserId);

        var departmentUuids = command.DepartmentIds.Distinct().ToList();
        var departmentIds = await _db.Departments
            .Where(d => departmentUuids.Contains(d.Uuid))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);

        if (departmentIds.Count != departmentUuids.Count)
        {
            throw new BusinessRuleException("Bir veya daha fazla departman bulunamadı.");
        }

        await _db.UserDepartments.Where(ud => ud.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);
        _db.UserDepartments.AddRange(departmentIds.Select(d => new UserDepartment { UserId = user.Id, DepartmentId = d }));
        await _db.SaveChangesAsync(cancellationToken);

        return await ToDetailAsync(user, cancellationToken);
    }

    private async Task AssignRolesInternalAsync(ApplicationUser user, IReadOnlyList<string> roles)
    {
        var requested = roles.Distinct(StringComparer.Ordinal).ToList();
        var known = await _db.Set<ApplicationRole>().Where(r => requested.Contains(r.Name!)).Select(r => r.Name!).ToListAsync();
        var unknown = requested.Except(known, StringComparer.Ordinal).ToList();
        if (unknown.Count > 0)
        {
            throw new BusinessRuleException($"Bilinmeyen rol(ler): {string.Join(", ", unknown)}");
        }

        var current = await _users.GetRolesAsync(user);
        var toRemove = current.Except(requested, StringComparer.Ordinal).ToList();
        var toAdd = requested.Except(current, StringComparer.Ordinal).ToList();

        if (toRemove.Count > 0)
        {
            ThrowIfFailed(await _users.RemoveFromRolesAsync(user, toRemove));
        }

        if (toAdd.Count > 0)
        {
            ThrowIfFailed(await _users.AddToRolesAsync(user, toAdd));
        }
    }

    private async Task<UserDetailResponse> ToDetailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await _users.GetRolesAsync(user);
        var departments = await _db.UserDepartments
            .Where(ud => ud.UserId == user.Id)
            .Select(ud => ud.Department!.Uuid)
            .ToListAsync(cancellationToken);

        return new UserDetailResponse(
            user.Uuid, user.UserName!, user.Email, user.DisplayName, user.LockoutEnabled, roles.ToList(), departments, user.CreatedAt);
    }

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new BusinessRuleException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
