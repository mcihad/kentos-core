using Kentos.Modules.Hesap.Application.Groups;
using Kentos.Modules.Hesap.Domain;
using Kentos.Modules.Hesap.Infrastructure;
using Kentos.SharedKernel.Exceptions;
using Kentos.SharedKernel.Pagination;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.Hesap.Services;

/// <summary>User group data operations + membership management.</summary>
public interface IUserGroupService
{
    Task<PagedResponse<GroupResponse>> ListAsync(ListGroupsQuery query, CancellationToken cancellationToken);
    Task<GroupResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<GroupResponse> CreateAsync(CreateGroupCommand command, CancellationToken cancellationToken);
    Task<GroupResponse> UpdateAsync(UpdateGroupCommand command, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<GroupMemberResponse>> ListMembersAsync(Guid id, CancellationToken cancellationToken);
    Task AddMemberAsync(AddGroupMemberCommand command, CancellationToken cancellationToken);
    Task RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken);
}

public sealed class UserGroupService : IUserGroupService
{
    private readonly HesapDbContext _db;

    public UserGroupService(HesapDbContext db) => _db = db;

    public async Task<PagedResponse<GroupResponse>> ListAsync(ListGroupsQuery query, CancellationToken cancellationToken)
    {
        var source = _db.UserGroups.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = $"%{query.Search.Trim()}%";
            source = source.Where(g => EF.Functions.ILike(g.Name, term));
        }

        var total = await source.LongCountAsync(cancellationToken);
        var items = await source
            .OrderBy(g => g.Name)
            .Skip(query.Skip).Take(query.PageSize)
            .ProjectToType<GroupResponse>()
            .ToListAsync(cancellationToken);

        return new PagedResponse<GroupResponse>(items, total, query.Page, query.PageSize);
    }

    public async Task<GroupResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.UserGroups.AsNoTracking()
            .Where(g => g.Uuid == id)
            .ProjectToType<GroupResponse>()
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFoundException.For("Grup", id);

    public async Task<GroupResponse> CreateAsync(CreateGroupCommand command, CancellationToken cancellationToken)
    {
        var group = new UserGroup { Name = command.Name, Description = command.Description };
        _db.UserGroups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(group.Uuid, cancellationToken);
    }

    public async Task<GroupResponse> UpdateAsync(UpdateGroupCommand command, CancellationToken cancellationToken)
    {
        var group = await _db.UserGroups.FirstOrDefaultAsync(g => g.Uuid == command.Id, cancellationToken)
            ?? throw NotFoundException.For("Grup", command.Id);

        group.Name = command.Name;
        group.Description = command.Description;
        await _db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(command.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var group = await _db.UserGroups.FirstOrDefaultAsync(g => g.Uuid == id, cancellationToken)
            ?? throw NotFoundException.For("Grup", id);

        await _db.UserGroupMembers.Where(m => m.GroupId == group.Id).ExecuteDeleteAsync(cancellationToken);
        _db.UserGroups.Remove(group);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GroupMemberResponse>> ListMembersAsync(Guid id, CancellationToken cancellationToken)
    {
        var groupId = await ResolveGroupIdAsync(id, cancellationToken);

        return await _db.UserGroupMembers.AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .OrderBy(m => m.User!.UserName)
            .Select(m => new GroupMemberResponse(m.User!.Uuid, m.User!.UserName!, m.User!.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task AddMemberAsync(AddGroupMemberCommand command, CancellationToken cancellationToken)
    {
        var groupId = await ResolveGroupIdAsync(command.GroupId, cancellationToken);
        var userId = await _db.Users.Where(u => u.Uuid == command.UserId).Select(u => (long?)u.Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFoundException.For("Kullanıcı", command.UserId);

        if (await _db.UserGroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken))
        {
            return;
        }

        _db.UserGroupMembers.Add(new UserGroupMember { GroupId = groupId, UserId = userId });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var groupId = await ResolveGroupIdAsync(id, cancellationToken);
        var internalUserId = await _db.Users.Where(u => u.Uuid == userId).Select(u => (long?)u.Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFoundException.For("Kullanıcı", userId);

        await _db.UserGroupMembers
            .Where(m => m.GroupId == groupId && m.UserId == internalUserId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task<long> ResolveGroupIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _db.UserGroups.Where(g => g.Uuid == id).Select(g => (long?)g.Id).FirstOrDefaultAsync(cancellationToken)
            ?? throw NotFoundException.For("Grup", id);
}
