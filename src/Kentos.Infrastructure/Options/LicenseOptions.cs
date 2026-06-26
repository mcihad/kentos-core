namespace Kentos.Infrastructure.Options;

/// <summary>License-driven enabled modules (the "License" section).</summary>
public sealed class LicenseOptions
{
    public const string SectionName = "License";

    /// <summary>Enabled module slugs (comma-separated), e.g. "settlement,billing".</summary>
    public string EnabledModules { get; set; } = "";

    /// <summary>Parsed list of enabled module slugs.</summary>
    public IReadOnlyList<string> Modules =>
        EnabledModules.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public bool IsEnabled(string slug) =>
        Modules.Contains(slug, StringComparer.OrdinalIgnoreCase);
}
