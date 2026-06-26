using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kentos.Modules.Hesap.Application.Me;
using Kentos.TestShared;
using Shouldly;

namespace Kentos.Modules.Hesap.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class HesapMeTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ApiFactory _factory;

    public HesapMeTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Me_is_unauthorized_without_token()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/hesap/me");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_returns_permissions_grouped_by_module()
    {
        var client = _factory.CreateClientWith("settlement.province.list", "hesap.user.list", "hesap.role.list");

        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/v1/hesap/me", Json);

        me!.Permissions.Keys.ShouldContain("settlement");
        me.Permissions.Keys.ShouldContain("hesap");
        me.Permissions["settlement"].ShouldContain("settlement.province.list");
        me.Permissions["hesap"].ShouldContain("hesap.user.list");
        me.Permissions["hesap"].ShouldContain("hesap.role.list");
    }
}
