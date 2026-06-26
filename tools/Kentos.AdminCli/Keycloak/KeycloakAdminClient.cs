using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kentos.AdminCli.Configuration;

namespace Kentos.AdminCli.Keycloak;

/// <summary>Thin, idempotent client over the Keycloak Admin REST API.</summary>
public sealed class KeycloakAdminClient
{
    private readonly HttpClient _http;
    private readonly KeycloakConfig _config;

    public KeycloakAdminClient(KeycloakConfig config)
    {
        _config = config;
        _http = new HttpClient { BaseAddress = new Uri(config.ServerUrl.TrimEnd('/') + "/") };
    }

    public async Task AuthenticateAsync(CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
            ["username"] = _config.AdminUser,
            ["password"] = _config.AdminPassword,
        });

        var response = await _http.PostAsync(
            $"realms/{_config.AdminRealm}/protocol/openid-connect/token", content, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonObject>(ct);
        var token = json?["access_token"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Keycloak admin token request returned no access_token.");

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<bool> EnsureRealmAsync(CancellationToken ct)
    {
        var existing = await _http.GetAsync($"admin/realms/{_config.Realm}", ct);
        if (existing.StatusCode == HttpStatusCode.OK)
        {
            return false;
        }

        var body = new JsonObject
        {
            ["realm"] = _config.Realm,
            ["enabled"] = true,
            ["displayName"] = "Kentos",
            ["sslRequired"] = "external",
            ["registrationAllowed"] = false,
        };

        if (!string.IsNullOrWhiteSpace(_config.LoginTheme))
        {
            body["loginTheme"] = _config.LoginTheme;
        }

        var response = await _http.PostAsJsonAsync("admin/realms", body, ct);
        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<(string Id, bool Created)> EnsureClientAsync(CancellationToken ct)
    {
        var existingId = await FindClientIdAsync(ct);
        if (existingId is not null)
        {
            return (existingId, false);
        }

        var body = new JsonObject
        {
            ["clientId"] = _config.ClientId,
            ["enabled"] = true,
            ["protocol"] = "openid-connect",
            ["publicClient"] = true,
            ["standardFlowEnabled"] = true,
            ["directAccessGrantsEnabled"] = true,
            ["redirectUris"] = ToArray(_config.RedirectUris),
            ["webOrigins"] = ToArray(_config.WebOrigins),
            ["attributes"] = new JsonObject { ["pkce.code.challenge.method"] = "S256" },
        };

        var response = await _http.PostAsJsonAsync($"admin/realms/{_config.Realm}/clients", body, ct);
        response.EnsureSuccessStatusCode();

        var id = await FindClientIdAsync(ct)
            ?? throw new InvalidOperationException("Created client could not be located.");
        return (id, true);
    }

    public async Task<bool> EnsureClientRoleAsync(string clientId, string roleName, string? description, CancellationToken ct)
    {
        var existing = await _http.GetAsync($"admin/realms/{_config.Realm}/clients/{clientId}/roles/{Uri.EscapeDataString(roleName)}", ct);
        if (existing.StatusCode == HttpStatusCode.OK)
        {
            return false;
        }

        var body = new JsonObject { ["name"] = roleName, ["description"] = description };
        var response = await _http.PostAsJsonAsync($"admin/realms/{_config.Realm}/clients/{clientId}/roles", body, ct);
        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<IReadOnlySet<string>> GetClientRoleNamesAsync(string clientId, CancellationToken ct)
    {
        var roles = await _http.GetFromJsonAsync<JsonArray>($"admin/realms/{_config.Realm}/clients/{clientId}/roles", ct);
        return roles?.Select(r => r!["name"]!.GetValue<string>()).ToHashSet(StringComparer.Ordinal)
            ?? new HashSet<string>(StringComparer.Ordinal);
    }

    public async Task<bool> EnsureProtocolMapperAsync(string clientId, string name, JsonObject mapper, CancellationToken ct)
    {
        var existing = await _http.GetFromJsonAsync<JsonArray>(
            $"admin/realms/{_config.Realm}/clients/{clientId}/protocol-mappers/models", ct);
        if (existing?.Any(m => m!["name"]!.GetValue<string>() == name) == true)
        {
            return false;
        }

        var response = await _http.PostAsJsonAsync(
            $"admin/realms/{_config.Realm}/clients/{clientId}/protocol-mappers/models", mapper, ct);
        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<(string Id, bool Created)> EnsureGroupAsync(string name, CancellationToken ct)
    {
        var groups = await _http.GetFromJsonAsync<JsonArray>(
            $"admin/realms/{_config.Realm}/groups?search={Uri.EscapeDataString(name)}", ct);
        var match = groups?.FirstOrDefault(g => g!["name"]!.GetValue<string>() == name);
        if (match is not null)
        {
            return (match["id"]!.GetValue<string>(), false);
        }

        var response = await _http.PostAsJsonAsync(
            $"admin/realms/{_config.Realm}/groups", new JsonObject { ["name"] = name }, ct);
        response.EnsureSuccessStatusCode();

        groups = await _http.GetFromJsonAsync<JsonArray>(
            $"admin/realms/{_config.Realm}/groups?search={Uri.EscapeDataString(name)}", ct);
        var created = groups!.First(g => g!["name"]!.GetValue<string>() == name);
        return (created!["id"]!.GetValue<string>(), true);
    }

    public async Task AssignClientRolesToGroupAsync(string groupId, string clientId, IEnumerable<string> roleNames, CancellationToken ct)
    {
        var roles = await _http.GetFromJsonAsync<JsonArray>(
            $"admin/realms/{_config.Realm}/clients/{clientId}/roles", ct);
        var wanted = roleNames.ToHashSet(StringComparer.Ordinal);
        var payload = new JsonArray(roles!
            .Where(r => wanted.Contains(r!["name"]!.GetValue<string>()))
            .Select(r => r!.DeepClone())
            .ToArray());

        var response = await _http.PostAsJsonAsync(
            $"admin/realms/{_config.Realm}/groups/{groupId}/role-mappings/clients/{clientId}", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> EnsureUserAsync(string username, string password, string groupName, CancellationToken ct)
    {
        var users = await _http.GetFromJsonAsync<JsonArray>(
            $"admin/realms/{_config.Realm}/users?username={Uri.EscapeDataString(username)}&exact=true", ct);

        string userId;
        bool created;
        if (users?.Count > 0)
        {
            userId = users[0]!["id"]!.GetValue<string>();
            created = false;
        }
        else
        {
            var body = new JsonObject
            {
                ["username"] = username,
                ["email"] = username,
                ["firstName"] = "Kentos",
                ["lastName"] = "Administrator",
                ["enabled"] = true,
                ["emailVerified"] = true,
                ["requiredActions"] = new JsonArray(),
                ["groups"] = new JsonArray("/" + groupName),
            };

            var response = await _http.PostAsJsonAsync($"admin/realms/{_config.Realm}/users", body, ct);
            response.EnsureSuccessStatusCode();

            users = await _http.GetFromJsonAsync<JsonArray>(
                $"admin/realms/{_config.Realm}/users?username={Uri.EscapeDataString(username)}&exact=true", ct);
            userId = users!.First()!["id"]!.GetValue<string>();
            created = true;
        }

        // Ensure the profile is complete (Keycloak 26 user profile requires names).
        var profile = new JsonObject
        {
            ["firstName"] = "Kentos",
            ["lastName"] = "Administrator",
            ["email"] = username,
            ["enabled"] = true,
            ["emailVerified"] = true,
            ["requiredActions"] = new JsonArray(),
        };
        var profileResponse = await _http.PutAsJsonAsync($"admin/realms/{_config.Realm}/users/{userId}", profile, ct);
        profileResponse.EnsureSuccessStatusCode();

        var credential = new JsonObject
        {
            ["type"] = "password",
            ["value"] = password,
            ["temporary"] = false,
        };
        var passwordResponse = await _http.PutAsJsonAsync(
            $"admin/realms/{_config.Realm}/users/{userId}/reset-password", credential, ct);
        passwordResponse.EnsureSuccessStatusCode();

        return created;
    }

    private async Task<string?> FindClientIdAsync(CancellationToken ct)
    {
        var clients = await _http.GetFromJsonAsync<JsonArray>(
            $"admin/realms/{_config.Realm}/clients?clientId={Uri.EscapeDataString(_config.ClientId)}", ct);
        var match = clients?.FirstOrDefault();
        return match?["id"]?.GetValue<string>();
    }

    private static JsonArray ToArray(IEnumerable<string> values) => new(values.Select(v => (JsonNode)v!).ToArray());
}
