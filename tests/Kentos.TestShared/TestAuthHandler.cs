using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kentos.TestShared;

/// <summary>
/// Test authentication handler. Reads the <c>X-Test-User</c> and
/// <c>X-Test-Permissions</c> headers to build a principal. Each value in the
/// permissions header becomes a <c>roles</c> claim; combined with the passthrough
/// resolver registered in <see cref="ApiFactory"/> (role name == permission key),
/// this lets integration tests exercise authorization without issuing real tokens.
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UserHeader = "X-Test-User";
    public const string PermissionsHeader = "X-Test-Permissions";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var user) || string.IsNullOrEmpty(user))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new("sub", user!),
            new("preferred_username", user!),
        };

        if (Request.Headers.TryGetValue(PermissionsHeader, out var permissions))
        {
            // Each permission is emitted as a role; the passthrough resolver maps role → permission 1:1.
            foreach (var permission in permissions.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim("roles", permission));
            }
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
