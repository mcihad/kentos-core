using Spectre.Console;

namespace Kentos.AdminCli;

/// <summary>Logs each successful provisioning stage to the console and a replayable log file.</summary>
internal static class Log
{
    private const string LogFile = "kentos-admin.log";

    public static void Ok(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(message)}");
        File.AppendAllText(LogFile, $"{DateTimeOffset.UtcNow:O}  OK  {message}{Environment.NewLine}");
    }

    public static void Add(string message) =>
        AnsiConsole.MarkupLine($"  [green]+[/] {Markup.Escape(message)}");

    public static void Info(string message) =>
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");
}
