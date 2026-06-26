using Kentos.AdminCli.Configuration;
using Kentos.AdminCli.Keycloak;
using Kentos.AdminCli.Permissions;
using Spectre.Console.Cli;

namespace Kentos.AdminCli.Commands;

/// <summary>Creates/updates the kentos-client roles from permissions.json (separate fast-dev command).</summary>
public sealed class PermissionsSyncCommand : AsyncCommand<PermissionsSyncCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-i|--input <PATH>")]
        public string Input { get; init; } = "permissions.json";
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var ct = cancellationToken;
        var file = PermissionScanner.LoadOrScan(settings.Input);
        var client = new KeycloakAdminClient(KeycloakConfig.FromEnvironment());

        await client.AuthenticateAsync(ct);
        Log.Ok("Authenticated to Keycloak");

        var (clientId, _) = await client.EnsureClientAsync(ct);
        var created = 0;
        foreach (var permission in file.Modules.SelectMany(m => m.Permissions))
        {
            if (await client.EnsureClientRoleAsync(clientId, permission.Key, permission.Title, ct))
            {
                created++;
                Log.Add(permission.Key);
            }
        }

        var total = file.Modules.Sum(m => m.Permissions.Count);
        Log.Ok($"Synced {total} permission role(s) ({created} created, {total - created} already present)");
        return 0;
    }
}
