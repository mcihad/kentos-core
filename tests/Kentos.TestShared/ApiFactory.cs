using Kentos.SharedKernel.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Kentos.TestShared;

/// <summary>
/// Spins up a PostGIS container and boots the real host against it, replacing the
/// self-issued JWT auth with <see cref="TestAuthHandler"/> and a passthrough
/// permission resolver (role name == permission key).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgis/postgis:18-3.6")
        .WithDatabase("kentos")
        .WithUsername("kentos")
        .WithPassword("kentos")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // NOTE: with WebApplicationFactory + minimal hosting, ConfigureAppConfiguration
        // does NOT override appsettings.json, so the test-container connection string is
        // injected via environment variables (higher precedence) in InitializeAsync.
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            // Last registration wins: resolve permissions directly from the role names
            // the test handler injects (1:1), bypassing the DB-backed resolver.
            services.AddSingleton<IPermissionResolver, PassthroughPermissionResolver>();
        });
    }

    /// <summary>Treats each role name as a permission key (test-only).</summary>
    private sealed class PassthroughPermissionResolver : IPermissionResolver
    {
        public IReadOnlySet<string> ResolvePermissions(IEnumerable<string> roles) =>
            roles.ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Creates a client authenticated as a test user carrying the given permissions.</summary>
    public HttpClient CreateClientWith(params string[] permissions)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, "test-user");
        if (permissions.Length > 0)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.PermissionsHeader, string.Join(',', permissions));
        }

        return client;
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();

        // Environment variables override appsettings.json reliably (unlike
        // ConfigureAppConfiguration under minimal hosting).
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", _postgres.GetConnectionString());
        Environment.SetEnvironmentVariable("License__EnabledModules", "settlement");
        Environment.SetEnvironmentVariable("Audit__Provider", "Postgres");
        Environment.SetEnvironmentVariable("OpenTelemetry__OtlpEndpoint", "");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
