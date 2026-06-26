using Kentos.AdminCli.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.SetApplicationName("kentos-admin");
    config.AddBranch("permissions", permissions =>
    {
        permissions.SetDescription("Permission catalog operations.");
        permissions.AddCommand<PermissionsScanCommand>("scan")
            .WithDescription("Scan module assemblies and (re)write permissions.json.");
    });
});

return await app.RunAsync(args);
