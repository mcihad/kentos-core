using System.Text.Json;
using Kentos.TestShared;
using Shouldly;

namespace Kentos.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class OpenApiDocumentTests
{
    private readonly ApiFactory _factory;

    public OpenApiDocumentTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Combined_document_uses_default_controller_tags_without_x_tagGroups()
    {
        var json = await _factory.CreateClient().GetStringAsync("/openapi/v1.json");
        using var doc = JsonDocument.Parse(json);

        // Modules are split into their own documents, so the combined document no longer
        // emits the x-tagGroups module grouping — operations carry the default
        // controller-name tag.
        doc.RootElement.TryGetProperty("x-tagGroups", out _).ShouldBeFalse();

        var listProvinces = doc.RootElement
            .GetProperty("paths").GetProperty("/api/v1/settlement/provinces")
            .GetProperty("get").GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToList();
        listProvinces.ShouldContain("Provinces");
    }

    [Fact]
    public async Task Per_module_document_only_contains_that_modules_routes()
    {
        var json = await _factory.CreateClient().GetStringAsync("/openapi/settlement.json");
        using var doc = JsonDocument.Parse(json);

        var paths = doc.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        paths.ShouldNotBeEmpty();
        paths.ShouldAllBe(p => p.StartsWith("/api/v1/settlement/"));
    }
}
