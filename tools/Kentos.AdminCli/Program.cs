using Kentos.AdminCli.Commands;
using Spectre.Console.Cli;

// Load .env (if present) so Keycloak settings flow into environment variables.
foreach (var candidate in new[] { ".env", Path.Combine("..", ".env"), Path.Combine("..", "..", ".env") })
{
    if (File.Exists(candidate))
    {
        DotNetEnv.Env.Load(candidate);
        break;
    }
}

var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("kentos-admin");
    config.AddCommand<ProvisionCommand>("provision")
        .WithDescription("Full idempotent Keycloak provisioning (realm, client, mappers, permissions, group, dev user).");
    config.AddBranch("permissions", permissions =>
    {
        permissions.SetDescription("Permission catalog operations.");
        permissions.AddCommand<PermissionsScanCommand>("scan")
            .WithDescription("Scan module assemblies and (re)write permissions.json.");
        permissions.AddCommand<PermissionsSyncCommand>("sync")
            .WithDescription("Create/update kentos-client roles from permissions.json.");
    });
});

return await app.RunAsync(args);
