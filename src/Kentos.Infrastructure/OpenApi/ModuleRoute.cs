namespace Kentos.Infrastructure.OpenApi;

/// <summary>
/// Parses module-scoped API routes shaped <c>api/v{n}/{module}/{resource}/...</c>.
/// Shared by the OpenAPI transformers and the per-module document filters so the
/// route convention lives in one place. Accepts both leading-slash and ApiExplorer
/// relative-path forms.
/// </summary>
public static class ModuleRoute
{
    /// <summary>Extracts <c>{module}</c> and <c>{resource}</c> segments, if the path matches.</summary>
    public static bool TryParse(string? path, out string slug, out string resource)
    {
        slug = "";
        resource = "";

        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // expected: ["api", "v1", "<module>", "<resource>", ...]
        if (segments.Length < 4
            || !string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase)
            || segments[1].Length < 2
            || char.ToLowerInvariant(segments[1][0]) != 'v')
        {
            return false;
        }

        slug = segments[2];
        resource = segments[3];
        return true;
    }

    /// <summary>True when the path belongs to the given module slug.</summary>
    public static bool BelongsTo(string? path, string slug) =>
        TryParse(path, out var s, out _) && string.Equals(s, slug, StringComparison.OrdinalIgnoreCase);
}
