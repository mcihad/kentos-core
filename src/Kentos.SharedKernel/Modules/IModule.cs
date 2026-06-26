using Kentos.SharedKernel.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kentos.SharedKernel.Modules;

/// <summary>
/// Contract for an application module. Modules never reference each other; the host
/// discovers them and registers only the ones enabled by license.
/// </summary>
public interface IModule
{
    /// <summary>Route segment (lowercase), e.g. "settlement".</summary>
    string Slug { get; }

    /// <summary>Human-readable display name, e.g. "Settlement".</summary>
    string DisplayName { get; }

    /// <summary>Module version (SemVer), e.g. "1.0.0".</summary>
    string Version { get; }

    /// <summary>License/feature key. <c>null</c> means a core module (always enabled).</summary>
    string? LicenseKey { get; }

    /// <summary>All permission definitions declared by the module.</summary>
    IReadOnlyList<PermissionDefinition> Permissions { get; }

    /// <summary>Registers the module's services, DbContext and infrastructure.</summary>
    void Register(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment);
}
