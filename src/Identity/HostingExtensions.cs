using GridPulse.Identity;
using Duende.IdentityServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using IdentityModel;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GridPulse.Identity;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddRazorPages();

        var isBuilder = builder.Services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/
                options.EmitStaticAudienceClaim = true;
            })
            .AddTestUsers(TestUsers.Users);

        // in-memory, code config
        isBuilder.AddInMemoryIdentityResources(Config.IdentityResources);
        isBuilder.AddInMemoryApiScopes(Config.ApiScopes);
        isBuilder.AddInMemoryApiResources(Config.ApiResources);
        isBuilder.AddInMemoryClients(Config.Clients(builder.Configuration));

        // Duende's default idsrv cookie is SameSite=None, which requires
        // Secure - but this deployment runs over plain HTTP locally (like
        // every other service here), so browsers silently drop it, making
        // login appear to succeed server-side and then immediately look
        // unauthenticated on the very next request. Lax is correct here
        // since this slice's login is a standard top-level redirect, not a
        // cross-site iframe scenario (checksession/front-channel logout),
        // which is the only reason None exists in the default template.
        builder.Services.Configure<CookieAuthenticationOptions>(
            IdentityServerConstants.DefaultCookieAuthenticationScheme,
            options => options.Cookie.SameSite = SameSiteMode.Lax);

        // Server-side session tracking: lists every currently logged-in
        // user (across all connected clients) with the ability to revoke a
        // session, backed by Duende's default in-memory store - no
        // Postgres needed, keeping Identity's "no database" scope intact.
        // Restricted to the dedicated "admin" test user's username - a
        // role claim was tried first, but Duende's local login cookie only
        // ever carries sub/name/idp/amr/auth_time (confirmed live), so
        // "name" (which equals the username on this cookie) is what's
        // actually checkable here, not any custom claim.
        isBuilder.AddServerSideSessions();

        builder.Services.AddAuthorization(options =>
            options.AddPolicy("admin", policy => policy.RequireClaim(JwtClaimTypes.Name, "admin")));
        builder.Services.Configure<RazorPagesOptions>(options =>
            options.Conventions.AuthorizeFolder("/ServerSideSessions", "admin"));


        return builder.Build();
    }
    
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();
        
        app.MapRazorPages()
            .RequireAuthorization();

        return app;
    }
}