using Kentos.SharedKernel.Modules;

namespace Kentos.Infrastructure.Modules;

/// <summary>Holds the enabled modules and produces their manifests for the metadata API.</summary>
public sealed class ModuleRegistry
{
    public ModuleRegistry(IReadOnlyList<IModule> enabledModules) => EnabledModules = enabledModules;

    public IReadOnlyList<IModule> EnabledModules { get; }

    public IReadOnlyList<ModuleManifest> GetManifests() =>
        EnabledModules.Select(ToManifest).ToList();

    public ModuleManifest? GetManifest(string slug) =>
        EnabledModules
            .Where(m => string.Equals(m.Slug, slug, StringComparison.OrdinalIgnoreCase))
            .Select(ToManifest)
            .FirstOrDefault();

    private static ModuleManifest ToManifest(IModule module) =>
        new(
            module.Slug,
            module.DisplayName,
            module.Version,
            Enabled: true,
            module.Permissions
                .Select(p => new ModulePermissionInfo(p.Key, p.Resource, p.Action, p.Title))
                .ToList());
}
