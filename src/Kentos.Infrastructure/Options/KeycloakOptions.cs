namespace Kentos.Infrastructure.Options;

/// <summary>Keycloak settings for API token validation (the "Keycloak" section).</summary>
public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";

    /// <summary>Realm authority, e.g. http://localhost:8080/realms/kentos.</summary>
    public string Authority { get; set; } = "";

    /// <summary>OIDC metadata address (derived from Authority when empty).</summary>
    public string? MetadataAddress { get; set; }

    /// <summary>Expected audience (client id).</summary>
    public string Audience { get; set; } = "kentos-client";

    /// <summary>Whether HTTPS metadata is required (false in dev).</summary>
    public bool RequireHttpsMetadata { get; set; }

    // For reference (the provisioning CLI binds these separately).
    public string ServerUrl { get; set; } = "http://localhost:8080";
    public string Realm { get; set; } = "kentos";
    public string ClientId { get; set; } = "kentos-client";
}
