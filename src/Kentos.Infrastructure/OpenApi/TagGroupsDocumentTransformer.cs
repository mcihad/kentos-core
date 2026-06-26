using System.Text.Json.Nodes;
using Kentos.Infrastructure.Modules;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Kentos.Infrastructure.OpenApi;

/// <summary>
/// Builds the Scalar sidebar as a <b>module &gt; resource &gt; method</b> tree.
/// Endpoint routes are structured <c>/api/v{n}/{module}/{resource}/...</c>, so each
/// operation is re-tagged with its <c>{resource}</c> and the resources are grouped by
/// module via the <c>x-tagGroups</c> extension (Scalar/Redoc convention). Module group
/// titles use the module's Turkish <c>DisplayName</c>.
/// </summary>
public sealed class TagGroupsDocumentTransformer : IOpenApiDocumentTransformer
{
    private readonly ModuleRegistry _modules;

    public TagGroupsDocumentTransformer(ModuleRegistry modules) => _modules = modules;

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (document.Paths is null)
        {
            return Task.CompletedTask;
        }

        var titleBySlug = _modules.EnabledModules.ToDictionary(m => m.Slug, m => m.DisplayName, StringComparer.OrdinalIgnoreCase);

        // module DisplayName → set of resource tag names
        var groups = new SortedDictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase);
        var allTags = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var (path, item) in document.Paths)
        {
            if (item?.Operations is null || !TryParse(path, titleBySlug, out var moduleTitle, out var resource))
            {
                continue;
            }

            foreach (var operation in item.Operations.Values)
            {
                operation.Tags = new HashSet<OpenApiTagReference> { new(resource, document) };
            }

            allTags.Add(resource);
            if (!groups.TryGetValue(moduleTitle, out var set))
            {
                groups[moduleTitle] = set = new SortedSet<string>(StringComparer.Ordinal);
            }

            set.Add(resource);
        }

        if (groups.Count == 0)
        {
            return Task.CompletedTask;
        }

        // Declare the tags on the document so references resolve.
        document.Tags ??= new HashSet<OpenApiTag>();
        var declared = document.Tags.Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var tag in allTags)
        {
            if (declared.Add(tag))
            {
                document.Tags.Add(new OpenApiTag { Name = tag });
            }
        }

        // x-tagGroups: [{ name: <module title>, tags: [<resource>, ...] }, ...]
        var groupsArray = new JsonArray();
        foreach (var (moduleTitle, tags) in groups)
        {
            var tagsArray = new JsonArray();
            foreach (var tag in tags)
            {
                tagsArray.Add(tag);
            }

            groupsArray.Add(new JsonObject { ["name"] = moduleTitle, ["tags"] = tagsArray });
        }

        document.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        document.Extensions["x-tagGroups"] = new JsonNodeExtension(groupsArray);

        return Task.CompletedTask;
    }

    /// <summary>Parses <c>/api/v{n}/{module}/{resource}/...</c> → (module title, resource).</summary>
    private static bool TryParse(
        string path,
        IReadOnlyDictionary<string, string> titleBySlug,
        out string moduleTitle,
        out string resource)
    {
        moduleTitle = "";
        resource = "";

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // expected: ["api", "v1", "<module>", "<resource>", ...]
        if (segments.Length < 4
            || !string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase)
            || segments[1].Length < 2
            || char.ToLowerInvariant(segments[1][0]) != 'v')
        {
            return false;
        }

        var slug = segments[2];
        if (!titleBySlug.TryGetValue(slug, out var title))
        {
            return false;
        }

        moduleTitle = title;
        resource = segments[3];
        return true;
    }
}
