using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Kentos.Modules.Hesap.Application.Permissions;
using Kentos.Modules.Hesap.Authentication;
using Kentos.Modules.Hesap.Permissions;
using Kentos.SharedKernel.Pagination;
using Kentos.TestShared;
using Shouldly;

namespace Kentos.Modules.Hesap.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class HesapAuthTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ApiFactory _factory;

    public HesapAuthTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_with_bootstrap_admin_returns_tokens_carrying_roles_only()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/hesap/auth/login", new LoginRequest("admin", "Admin!234"), Json);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(Json);
        tokens!.AccessToken.ShouldNotBeNullOrWhiteSpace();
        tokens.RefreshToken.ShouldNotBeNullOrWhiteSpace();

        var payload = DecodeJwtPayload(tokens.AccessToken);

        // Roles are present...
        var roles = ReadStringValues(payload, "roles");
        roles.ShouldContain("yonetici");

        // ...and permissions are NOT embedded in the token (the whole point).
        payload.ContainsKey("permissions").ShouldBeFalse();
    }

    [Fact]
    public async Task Login_with_wrong_password_is_unauthorized()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/hesap/auth/login", new LoginRequest("admin", "wrong-password"), Json);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_rotates_token_and_invalidates_the_used_one()
    {
        var client = _factory.CreateClient();

        var login = await (await client.PostAsJsonAsync(
            "/api/v1/hesap/auth/login", new LoginRequest("admin", "Admin!234"), Json))
            .Content.ReadFromJsonAsync<TokenResponse>(Json);

        var refreshed = await client.PostAsJsonAsync(
            "/api/v1/hesap/auth/refresh", new RefreshRequest(login!.RefreshToken), Json);
        refreshed.StatusCode.ShouldBe(HttpStatusCode.OK);

        // The original refresh token must no longer be accepted (rotation).
        var reused = await client.PostAsJsonAsync(
            "/api/v1/hesap/auth/refresh", new RefreshRequest(login.RefreshToken), Json);
        reused.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Permission_catalog_is_seeded_at_startup()
    {
        // The TestAuthHandler maps each header value to a role; the passthrough resolver
        // turns it into the matching permission, so this exercises the seeded catalog.
        var client = _factory.CreateClientWith(HesapPermissions.Permission.List);

        var page = await client.GetFromJsonAsync<PagedResponse<PermissionResponse>>(
            "/api/v1/hesap/permissions?pageSize=200", Json);

        page!.TotalCount.ShouldBeGreaterThan(0);
        page.Items.ShouldContain(p => p.Key == HesapPermissions.Role.Create);
        page.Items.ShouldContain(p => p.Key.StartsWith("settlement."));
    }

    private static Dictionary<string, JsonElement> DecodeJwtPayload(string jwt)
    {
        var part = jwt.Split('.')[1];
        var padded = part.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)!;
    }

    private static List<string> ReadStringValues(Dictionary<string, JsonElement> payload, string key)
    {
        if (!payload.TryGetValue(key, out var element))
        {
            return [];
        }

        return element.ValueKind == JsonValueKind.Array
            ? element.EnumerateArray().Select(e => e.GetString()!).ToList()
            : [element.GetString()!];
    }
}
