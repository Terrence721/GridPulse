using Duende.IdentityServer.Models;
using Microsoft.Extensions.Configuration;

namespace GridPulse.Identity;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new ApiScope("grid-ops-api", "Grid Operations Console API"),
        };

    // Without an explicit ApiResource, Duende falls back to a single
    // static "{issuer}/resources" audience claim on every token
    // (confirmed live: a real token's aud was "http://localhost:5105/
    // resources", not "grid-ops-api") - the gateway's JWT bearer config
    // expects the audience to literally be "grid-ops-api", so this
    // resource has to exist for that check to ever pass.
    public static IEnumerable<ApiResource> ApiResources =>
        new ApiResource[]
        {
            new ApiResource("grid-ops-api", "Grid Operations Console API")
            {
                Scopes = { "grid-ops-api" }
            }
        };

    public static IEnumerable<Client> Clients(IConfiguration configuration) =>
        new Client[]
        {
            // The Grid Operations console SPA - Authorization Code + PKCE, no
            // secret since a browser app can't keep one confidential.
            new Client
            {
                ClientId = "grid-ops-console",
                ClientName = "Grid Operations Console",

                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,

                RedirectUris = { "http://localhost:5173/callback" },
                PostLogoutRedirectUris = { "http://localhost:5173/" },
                AllowedCorsOrigins = { "http://localhost:5173" },

                AllowedScopes = { "openid", "profile", "grid-ops-api" },
                AccessTokenLifetime = 3600,
            },

            // Machine-to-machine client used only by the automated Aspire
            // smoke test to get a real token without simulating an
            // interactive browser login.
            new Client
            {
                ClientId = "grid-ops-console-smoke-test",
                ClientName = "Grid Operations Console Smoke Test",

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret(configuration["SmokeTestClient:Secret"]!.Sha256()) },

                AllowedScopes = { "grid-ops-api" },
            },
        };
}
