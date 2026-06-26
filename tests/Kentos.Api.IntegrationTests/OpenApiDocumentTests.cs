using System.Text.Json;
using Kentos.TestShared;
using Shouldly;

namespace Kentos.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class OpenApiTagGroupsTests
{
    private readonly ApiFactory _factory;

    public OpenApiTagGroupsTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task OpenApi_document_groups_resources_by_module_via_x_tagGroups()
    {
        var json = await _factory.CreateClient().GetStringAsync("/openapi/v1.json");
        using var doc = JsonDocument.Parse(json);

        var groups = doc.RootElement.GetProperty("x-tagGroups").EnumerateArray().ToList();
        groups.ShouldNotBeEmpty();

        // Module group titles use the Turkish DisplayName.
        var hesap = groups.Single(g => g.GetProperty("name").GetString() == "Hesap");
        var hesapTags = hesap.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToList();
        hesapTags.ShouldContain("users");
        hesapTags.ShouldContain("roles");

        var settlement = groups.Single(g => g.GetProperty("name").GetString() == "Yerleşim");
        settlement.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ShouldContain("provinces");

        // An operation is tagged with its resource (so it nests under the module group).
        var listProvinces = doc.RootElement
            .GetProperty("paths").GetProperty("/api/v1/settlement/provinces")
            .GetProperty("get").GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToList();
        listProvinces.ShouldContain("provinces");
    }
}
