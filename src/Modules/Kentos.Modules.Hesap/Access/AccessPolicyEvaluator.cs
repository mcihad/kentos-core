using System.Net;
using System.Net.Sockets;
using Kentos.Modules.Hesap.Domain;
using Kentos.Modules.Hesap.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Kentos.Modules.Hesap.Access;

/// <summary>
/// Evaluates access policies that target the user directly or via group membership.
/// Rules: a matching Deny always blocks. For a kind (IP/time) that has at least one
/// Allow policy, the request must match one of them (allow-list); kinds without any
/// Allow policy are unrestricted. Evaluated only at login.
/// </summary>
public sealed class AccessPolicyEvaluator : IAccessPolicyEvaluator
{
    private readonly HesapDbContext _db;
    private readonly TimeProvider _clock;

    public AccessPolicyEvaluator(HesapDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<AccessDecision> EvaluateLoginAsync(long userId, string? ipAddress, CancellationToken cancellationToken)
    {
        var groupIds = await _db.UserGroupMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId)
            .ToListAsync(cancellationToken);

        var policies = await _db.AccessPolicies
            .AsNoTracking()
            .Where(p =>
                (p.SubjectType == PolicySubjectType.User && p.SubjectId == userId) ||
                (p.SubjectType == PolicySubjectType.Group && groupIds.Contains(p.SubjectId)))
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);

        if (policies.Count == 0)
        {
            return AccessDecision.Allow();
        }

        var now = _clock.GetLocalNow().TimeOfDay;
        var ip = TryParseIp(ipAddress);

        foreach (var kind in new[] { PolicyKind.Ip, PolicyKind.Time })
        {
            var ofKind = policies.Where(p => p.Kind == kind).ToList();
            if (ofKind.Count == 0)
            {
                continue;
            }

            var decision = EvaluateKind(kind, ofKind, ip, now);
            if (!decision.Allowed)
            {
                return decision;
            }
        }

        return AccessDecision.Allow();
    }

    private static AccessDecision EvaluateKind(PolicyKind kind, List<AccessPolicy> policies, IPAddress? ip, TimeSpan now)
    {
        var hasAllow = false;
        var matchedAllow = false;

        foreach (var policy in policies)
        {
            var matches = kind == PolicyKind.Ip ? MatchesIp(policy.Value, ip) : MatchesTime(policy.Value, now);

            if (policy.Effect == PolicyEffect.Deny && matches)
            {
                return AccessDecision.Deny(
                    kind == PolicyKind.Ip ? "IP adresi erişime kapalı." : "Bu saatte erişime izin verilmiyor.");
            }

            if (policy.Effect == PolicyEffect.Allow)
            {
                hasAllow = true;
                matchedAllow |= matches;
            }
        }

        if (hasAllow && !matchedAllow)
        {
            return AccessDecision.Deny(
                kind == PolicyKind.Ip ? "IP adresi izinli aralıkların dışında." : "Giriş saati izinli aralıkların dışında.");
        }

        return AccessDecision.Allow();
    }

    private static IPAddress? TryParseIp(string? ipAddress) =>
        IPAddress.TryParse(ipAddress, out var ip) ? ip : null;

    private static bool MatchesIp(string cidr, IPAddress? ip)
    {
        if (ip is null)
        {
            return false;
        }

        var parts = cidr.Split('/', 2);
        if (!IPAddress.TryParse(parts[0], out var network))
        {
            return false;
        }

        if (network.AddressFamily != ip.AddressFamily)
        {
            return false;
        }

        if (parts.Length == 1)
        {
            return network.Equals(ip);
        }

        if (!int.TryParse(parts[1], out var prefix))
        {
            return false;
        }

        var networkBytes = network.GetAddressBytes();
        var ipBytes = ip.GetAddressBytes();
        var totalBits = networkBytes.Length * 8;
        if (prefix < 0 || prefix > totalBits)
        {
            return false;
        }

        for (var bit = 0; bit < prefix; bit++)
        {
            var index = bit / 8;
            var mask = (byte)(1 << (7 - (bit % 8)));
            if ((networkBytes[index] & mask) != (ipBytes[index] & mask))
            {
                return false;
            }
        }

        return true;
    }

    private static bool MatchesTime(string window, TimeSpan now)
    {
        var parts = window.Split('-', 2);
        if (parts.Length != 2
            || !TimeSpan.TryParse(parts[0], out var start)
            || !TimeSpan.TryParse(parts[1], out var end))
        {
            return false;
        }

        // Wrapping window (e.g. 22:00-06:00) spans midnight.
        return start <= end ? now >= start && now < end : now >= start || now < end;
    }
}
