using Kentos.Infrastructure.Options;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Kentos.Infrastructure.OpenApi;

/// <summary>Adds a Keycloak OAuth2 (Authorization Code + PKCE) security scheme to the document.</summary>
public sealed class KeycloakSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private readonly KeycloakOptions _keycloak;

    public KeycloakSecuritySchemeTransformer(IOptions<KeycloakOptions> keycloak) => _keycloak = keycloak.Value;

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_keycloak.Authority))
        {
            return Task.CompletedTask;
        }

        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description = "Keycloak (Authorization Code + PKCE)",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{_keycloak.Authority}/protocol/openid-connect/auth"),
                    TokenUrl = new Uri($"{_keycloak.Authority}/protocol/openid-connect/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        ["openid"] = "OpenID",
                        ["profile"] = "Profile",
                    },
                },
            },
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["keycloak"] = scheme;

        return Task.CompletedTask;
    }
}
