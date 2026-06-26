using System.Text.Json.Nodes;

namespace Kentos.AdminCli.Keycloak;

/// <summary>Protocol mapper representations for the kentos client.</summary>
public static class KeycloakMappers
{
    public static JsonObject Audience(string clientId) => new()
    {
        ["name"] = $"audience-{clientId}",
        ["protocol"] = "openid-connect",
        ["protocolMapper"] = "oidc-audience-mapper",
        ["config"] = new JsonObject
        {
            ["included.client.audience"] = clientId,
            ["id.token.claim"] = "false",
            ["access.token.claim"] = "true",
        },
    };

    public static JsonObject ClientRolesToPermissions(string clientId) => new()
    {
        ["name"] = "client-roles-permissions",
        ["protocol"] = "openid-connect",
        ["protocolMapper"] = "oidc-usermodel-client-role-mapper",
        ["config"] = new JsonObject
        {
            ["usermodel.clientRoleMapping.clientId"] = clientId,
            ["claim.name"] = "permissions",
            ["jsonType.label"] = "String",
            ["multivalued"] = "true",
            ["id.token.claim"] = "false",
            ["access.token.claim"] = "true",
            ["userinfo.token.claim"] = "false",
        },
    };
}
