using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kentos.SharedKernel.Modules;
using Kentos.TestShared;
using Shouldly;

namespace Kentos.Api.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class PlatformApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ApiFactory _factory;

    public PlatformApiTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Metadata_lists_the_enabled_settlement_module()
    {
        var modules = await _factory.CreateClient().GetFromJsonAsync<List<ModuleManifest>>("/api/v1/metadata", Json);

        modules.ShouldNotBeNull();
        modules!.ShouldContain(m => m.Slug == "settlement");
        modules.Single(m => m.Slug == "settlement").Permissions.ShouldContain(p => p.Key == "settlement.neighborhood.create");
    }

    [Fact]
    public async Task Health_live_returns_ok()
    {
        var response = await _factory.CreateClient().GetAsync("/health/live");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Metrics_endpoint_is_exposed()
    {
        var body = await _factory.CreateClient().GetStringAsync("/metrics");
        body.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task OpenApi_document_carries_required_permission_extension()
    {
        var document = await _factory.CreateClient().GetStringAsync("/openapi/v1.json");
        document.ShouldContain("x-required-permission");
        document.ShouldContain("settlement.neighborhood.create");
    }
}
