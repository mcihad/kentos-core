namespace Kentos.Infrastructure.Options;

/// <summary>CORS settings (the "Cors" section).</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "KentosCors";

    /// <summary>Allowed origins (comma-separated).</summary>
    public string AllowedOrigins { get; set; } = "";

    public IReadOnlyList<string> Origins =>
        AllowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
