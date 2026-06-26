using System.Reflection;
using System.Text.Json;
using Kentos.SharedKernel.Modules;

namespace Kentos.AdminCli.Permissions;

/// <summary>Discovers all modules (license-independent) and builds the permissions file.</summary>
public static class PermissionScanner
{
    /// <summary>Loads the permissions file from disk, or scans modules if it does not exist.</summary>
    public static PermissionsFile LoadOrScan(string path)
    {
        if (!File.Exists(path))
        {
            return Build();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<PermissionsFile>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
            ?? Build();
    }

    public static PermissionsFile Build()
    {
        var modules = DiscoverModules()
            .OrderBy(m => m.Slug, StringComparer.Ordinal)
            .Select(m => new PermissionModule(
                m.Slug,
                m.DisplayName,
                m.Permissions
                    .Select(p => new PermissionEntry(p.Key, p.Module, p.Resource, p.Action, p.Title, p.Description))
                    .ToList()))
            .ToList();

        return new PermissionsFile(DateTimeOffset.UtcNow.ToString("O"), modules);
    }

    private static IEnumerable<IModule> DiscoverModules()
    {
        foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "Kentos.Modules.*.dll"))
        {
            try
            {
                Assembly.LoadFrom(dll);
            }
            catch
            {
                // Ignore unloadable assemblies.
            }
        }

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(SafeGetTypes)
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IModule).IsAssignableFrom(t))
            .Select(t => (IModule)Activator.CreateInstance(t)!);
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
