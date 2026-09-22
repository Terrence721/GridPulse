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
            new("roles", "Dispatcher/Admin role", new[] { "role" }),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            new("grid-ops-api", "Grid Operations Console API")
            {
                // Access-token claims are governed by the ApiScope/ApiResource's own
                // UserClaims, not by IdentityResources - those only flow into the ID
                // token/userinfo response. Without this, the real access token sent
                // to the gateway carried no "role" claim at all despite the "roles"
                // IdentityResource - confirmed live, RequireRole() correctly found
                // nothing to match and rejected every dispatcher request with 403.
                UserClaims = { "role" }
            },
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
            new("grid-ops-api", "Grid Operations Console API")
            {
                Scopes = { "grid-ops-api" }
            }
        };

    public static IEnumerable<Client> Clients(IConfiguration configuration) =>
        new Client[]
        {
            // The Grid Operations console SPA - Authorization Code + PKCE, no
            // secret since a browser app can't keep one confidential.
            new()
            {
                ClientId = "grid-ops-console",
                ClientName = "Grid Operations Console",

                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,

                RedirectUris = { "http://localhost:5107/callback" },
                PostLogoutRedirectUris = { "http://localhost:5107/" },
                AllowedCorsOrigins = { "http://localhost:5107" },

                AllowedScopes = { "openid", "profile", "roles", "grid-ops-api" },
                AccessTokenLifetime = 3600,
            },

            // Machine-to-machine client used only by the automated Aspire
            // smoke test to get a real token without simulating an
            // interactive browser login.
            new()
            {
                ClientId = "grid-ops-console-smoke-test",
                ClientName = "Grid Operations Console Smoke Test",

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret((configuration["SmokeTestClient:Secret"]
                    ?? throw new InvalidOperationException("SmokeTestClient:Secret must be set (the grid-ops-console-smoke-test client's secret).")).Sha256()) },

                AllowedScopes = { "grid-ops-api" },
            },
        };
}
