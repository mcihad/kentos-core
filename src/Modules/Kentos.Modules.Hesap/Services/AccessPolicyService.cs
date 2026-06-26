using Kentos.Modules.Hesap.Application.Policies;
using Kentos.Modules.Hesap.Domain;
using Kentos.Modules.Hesap.Infrastructure;
using Kentos.SharedKernel.Exceptions;
using Kentos.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.Hesap.Services;

/// <summary>
/// Access policy operations. The subject is polymorphic (user or group) and stored by
/// internal id, so responses are composed here (Uuid ↔ internal id) rather than via Mapster.
/// </summary>
public interface IAccessPolicyService
{
    Task<PagedResponse<PolicyResponse>> ListAsync(ListPoliciesQuery query, CancellationToken cancellationToken);
    Task<PolicyResponse> CreateAsync(CreatePolicyCommand command, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class AccessPolicyService : IAccessPolicyService
{
    private readonly HesapDbContext _db;

    public AccessPolicyService(HesapDbContext db) => _db = db;

    public async Task<PagedResponse<PolicyResponse>> ListAsync(ListPoliciesQuery query, CancellationToken cancellationToken)
    {
        var source = _db.AccessPolicies.AsNoTracking();

        var total = await source.LongCountAsync(cancellationToken);
        var rows = await source
            .OrderBy(p => p.Priority).ThenBy(p => p.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var userIds = rows.Where(r => r.SubjectType == PolicySubjectType.User).Select(r => r.SubjectId).ToList();
        var groupIds = rows.Where(r => r.SubjectType == PolicySubjectType.Group).Select(r => r.SubjectId).ToList();

        var userUuids = await _db.Users.Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Uuid, cancellationToken);
        var groupUuids = await _db.UserGroups.Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Uuid, cancellationToken);

        var items = rows.Select(r => new PolicyResponse(
            r.Uuid,
            r.SubjectType,
            r.SubjectType == PolicySubjectType.User
                ? userUuids.GetValueOrDefault(r.SubjectId)
                : groupUuids.GetValueOrDefault(r.SubjectId),
            r.Kind,
            r.Effect,
            r.Value,
            r.Priority)).ToList();

        return new PagedResponse<PolicyResponse>(items, total, query.Page, query.PageSize);
    }

    public async Task<PolicyResponse> CreateAsync(CreatePolicyCommand command, CancellationToken cancellationToken)
    {
        var subjectId = command.SubjectType == PolicySubjectType.User
            ? await _db.Users.Where(u => u.Uuid == command.SubjectId).Select(u => (long?)u.Id).FirstOrDefaultAsync(cancellationToken)
                ?? throw NotFoundException.For("Kullanıcı", command.SubjectId)
            : await _db.UserGroups.Where(g => g.Uuid == command.SubjectId).Select(g => (long?)g.Id).FirstOrDefaultAsync(cancellationToken)
                ?? throw NotFoundException.For("Grup", command.SubjectId);

        var policy = new AccessPolicy
        {
            SubjectType = command.SubjectType,
            SubjectId = subjectId,
            Kind = command.Kind,
            Effect = command.Effect,
            Value = command.Value,
            Priority = command.Priority,
        };

        _db.AccessPolicies.Add(policy);
        await _db.SaveChangesAsync(cancellationToken);

        return new PolicyResponse(policy.Uuid, policy.SubjectType, command.SubjectId, policy.Kind, policy.Effect, policy.Value, policy.Priority);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var policy = await _db.AccessPolicies.FirstOrDefaultAsync(p => p.Uuid == id, cancellationToken)
            ?? throw NotFoundException.For("Erişim politikası", id);

        _db.AccessPolicies.Remove(policy);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
