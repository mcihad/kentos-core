using Kentos.AdminCli.Configuration;
using Kentos.AdminCli.Keycloak;
using Kentos.AdminCli.Permissions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Kentos.AdminCli.Commands;

/// <summary>Full idempotent Keycloak provisioning (realm, client, mappers, permissions, group, dev user).</summary>
public sealed class ProvisionCommand : AsyncCommand<ProvisionCommand.Settings>
{
    private const string AdminGroup = "kentos-administrators";

    public sealed class Settings : CommandSettings
    {
        [CommandOption("--dev")]
        public bool Dev { get; init; }
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var ct = cancellationToken;
        var config = KeycloakConfig.FromEnvironment();
        var client = new KeycloakAdminClient(config);

        Log.Info($"Provisioning Keycloak at {config.ServerUrl} (realm '{config.Realm}', client '{config.ClientId}')");

        await client.AuthenticateAsync(ct);
        Log.Ok("Authenticated to master realm");

        var realmCreated = await client.EnsureRealmAsync(ct);
        Log.Ok(realmCreated ? $"Created realm '{config.Realm}'" : $"Realm '{config.Realm}' already present");

        var (clientId, clientCreated) = await client.EnsureClientAsync(ct);
        Log.Ok(clientCreated ? $"Created client '{config.ClientId}'" : $"Client '{config.ClientId}' already present");

        await client.EnsureProtocolMapperAsync(clientId, $"audience-{config.ClientId}", KeycloakMappers.Audience(config.ClientId), ct);
        Log.Ok("Ensured audience protocol mapper");

        await client.EnsureProtocolMapperAsync(clientId, "client-roles-permissions", KeycloakMappers.ClientRolesToPermissions(config.ClientId), ct);
        Log.Ok("Ensured client-roles -> 'permissions' claim mapper");

        var file = PermissionScanner.LoadOrScan("permissions.json");
        var permissions = file.Modules.SelectMany(m => m.Permissions).ToList();
        var created = 0;
        foreach (var permission in permissions)
        {
            if (await client.EnsureClientRoleAsync(clientId, permission.Key, permission.Title, ct))
            {
                created++;
            }
        }

        Log.Ok($"Synced {permissions.Count} permission role(s) ({created} created)");

        var (groupId, groupCreated) = await client.EnsureGroupAsync(AdminGroup, ct);
        await client.AssignClientRolesToGroupAsync(groupId, clientId, permissions.Select(p => p.Key), ct);
        Log.Ok(groupCreated
            ? $"Created group '{AdminGroup}' and assigned all permissions"
            : $"Group '{AdminGroup}' present; assigned all permissions");

        if (settings.Dev)
        {
            var userCreated = await client.EnsureUserAsync(config.DevTestUser, config.DevTestPassword, AdminGroup, ct);
            Log.Ok(userCreated
                ? $"Created dev user '{config.DevTestUser}' (member of {AdminGroup})"
                : $"Dev user '{config.DevTestUser}' already present");
        }

        AnsiConsole.MarkupLine("[bold green]Provisioning complete.[/]");
        return 0;
    }
}
