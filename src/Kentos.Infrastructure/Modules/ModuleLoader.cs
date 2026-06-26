using System.Reflection;
using Kentos.Infrastructure.Options;
using Kentos.SharedKernel.Modules;
using Microsoft.Extensions.Configuration;

namespace Kentos.Infrastructure.Modules;

/// <summary>
/// Discovers <see cref="IModule"/> implementations from the deployed assemblies and
/// keeps only the ones enabled by license.
/// </summary>
public static class ModuleLoader
{
    public static IReadOnlyList<IModule> DiscoverEnabledModules(IConfiguration configuration)
    {
        var license = configuration.GetSection(LicenseOptions.SectionName).Get<LicenseOptions>() ?? new LicenseOptions();
        LoadModuleAssemblies();

        var modules = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic)
            .SelectMany(SafeGetTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IModule).IsAssignableFrom(type))
            .Select(type => (IModule)Activator.CreateInstance(type)!)
            .Where(module => module.LicenseKey is null || license.IsEnabled(module.Slug))
            .OrderBy(module => module.Slug, StringComparer.Ordinal)
            .ToList();

        return modules;
    }

    private static void LoadModuleAssemblies()
    {
        foreach (var dll in Directory.GetFiles(AppContext.BaseDirectory, "Kentos.Modules.*.dll"))
        {
            try
            {
                Assembly.LoadFrom(dll);
            }
            catch
            {
                // Ignore assemblies that cannot be loaded.
            }
        }
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }
}
