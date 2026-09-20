using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Hosting;

// Opt-in gateway concerns (CORS, JWT-bearer auth), kept separate from
// AddServiceDefaults() - that one is called unconditionally by all 7
// backend services today, including ones that must never grow CORS/auth
// surface. A separate, explicitly-called extension gets real reuse for a
// future customer-dashboard BFF too, without touching the shared default
// path every other service depends on.
public static class GatewayExtensions
{
    public static TBuilder AddGatewayCors<TBuilder>(this TBuilder builder, string policyName, string[] allowedOrigins)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddCors(o => o.AddPolicy(policyName, p =>
            p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

        return builder;
    }

    public static TBuilder AddGatewayAuthentication<TBuilder>(this TBuilder builder, string audience)
        where TBuilder : IHostApplicationBuilder
    {
        var authority = builder.Configuration["services:identity:http:0"]
            ?? throw new InvalidOperationException("Identity endpoint not configured.");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = false; // dev-only container network; revisit before any real deployment
                options.Events = new JwtBearerEvents
                {
                    // SignalR's WebSocket/SSE transports can't set an
                    // Authorization header on the handshake - the JS client
                    // instead appends the token as ?access_token=..., which
                    // JwtBearer ignores by default.
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization();

        return builder;
    }
}
