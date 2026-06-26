using System.Text.Json;
using Kentos.AdminCli.Permissions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kentos.AdminCli.Commands;

/// <summary>Scans module assemblies and (re)writes permissions.json.</summary>
public sealed class PermissionsScanCommand : AsyncCommand<PermissionsScanCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-o|--output <PATH>")]
        public string Output { get; init; } = "permissions.json";
    }

    protected override Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var file = PermissionScanner.Build();
        var json = JsonSerializer.Serialize(file, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true });
        File.WriteAllText(settings.Output, json);

        var count = file.Modules.Sum(m => m.Permissions.Count);
        Log.Ok($"Wrote {count} permissions across {file.Modules.Count} module(s) to {settings.Output}");
        return Task.FromResult(0);
    }
}
