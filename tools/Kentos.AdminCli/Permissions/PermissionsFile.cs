namespace Kentos.AdminCli.Permissions;

public sealed record PermissionsFile(string GeneratedAt, IReadOnlyList<PermissionModule> Modules);

public sealed record PermissionModule(string Slug, string DisplayName, IReadOnlyList<PermissionEntry> Permissions);

public sealed record PermissionEntry(
    string Key,
    string Module,
    string Resource,
    string Action,
    string Title,
    string? Description);
