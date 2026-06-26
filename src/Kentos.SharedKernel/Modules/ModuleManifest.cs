namespace Kentos.SharedKernel.Modules;

/// <summary>Module manifest returned by the metadata API.</summary>
public sealed record ModuleManifest(
    string Slug,
    string DisplayName,
    string Version,
    bool Enabled,
    IReadOnlyList<ModulePermissionInfo> Permissions);

/// <summary>A single permission summary inside a manifest.</summary>
public sealed record ModulePermissionInfo(
    string Key,
    string Resource,
    string Action,
    string Title);
