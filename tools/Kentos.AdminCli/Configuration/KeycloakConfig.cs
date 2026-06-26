namespace Kentos.AdminCli.Configuration;

/// <summary>Keycloak provisioning settings, read from environment variables (.env).</summary>
public sealed class KeycloakConfig
{
    public required string ServerUrl { get; init; }
    public required string AdminRealm { get; init; }
    public required string AdminUser { get; init; }
    public required string AdminPassword { get; init; }
    public required string Realm { get; init; }
    public required string ClientId { get; init; }
    public required string LoginTheme { get; init; }
    public required string[] RedirectUris { get; init; }
    public required string[] WebOrigins { get; init; }
    public required string DevTestUser { get; init; }
    public required string DevTestPassword { get; init; }

    public static KeycloakConfig FromEnvironment() => new()
    {
        ServerUrl = Get("Keycloak__ServerUrl", "http://localhost:8080"),
        AdminRealm = Get("Keycloak__AdminRealm", "master"),
        AdminUser = Get("Keycloak__AdminUser", "admin"),
        AdminPassword = Get("Keycloak__AdminPassword", "admin"),
        Realm = Get("Keycloak__Realm", "kentos"),
        ClientId = Get("Keycloak__ClientId", "kentos-client"),
        // Empty = Keycloak default theme. Set Keycloak__LoginTheme=kentos to use the
        // custom theme (currently incompatible with Keycloak 26.x — see README).
        LoginTheme = Get("Keycloak__LoginTheme", ""),
        RedirectUris = Split(Get("Keycloak__RedirectUris", "http://localhost:5080/scalar/*,http://localhost:5080/*")),
        WebOrigins = Split(Get("Keycloak__WebOrigins", "http://localhost:5080")),
        DevTestUser = Get("Keycloak__DevTestUser", "admin@kentos"),
        DevTestPassword = Get("Keycloak__DevTestPassword", "Admin!234"),
    };

    private static string Get(string key, string fallback) =>
        Environment.GetEnvironmentVariable(key) is { Length: > 0 } value ? value : fallback;

    private static string[] Split(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
