// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using IdentityModel;
using System.Security.Claims;
using Duende.IdentityServer.Test;

namespace GridPulse.Identity;

// Dev-only demo credentials. The 4 dispatcher accounts share the
// "dispatcher" role (Outages + Work Orders access); a separate
// console-admin account carries the "admin" role (Outages only, no
// Work Orders). The 5th account, literally named "admin", is unrelated
// to either of those roles: it's an Identity-Server-administration
// identity, not a console persona - Duende's Server-Side Sessions page
// is gated on that username directly, since custom TestUser claims (a
// role claim included) never reach the local login cookie - only
// sub/name/idp/amr/auth_time do, confirmed live.
public static class TestUsers
{
    public static List<TestUser> Users =>
        new()
        {
            new TestUser
            {
                SubjectId = "1",
                Username = "jordan.alvarez",
                Password = "DispatcherDemo!1",
                Claims =
                {
                    new Claim(JwtClaimTypes.Name, "Jordan Alvarez"),
                    new Claim(JwtClaimTypes.GivenName, "Jordan"),
                    new Claim(JwtClaimTypes.FamilyName, "Alvarez"),
                    new Claim(JwtClaimTypes.Email, "jordan.alvarez@gridpulse.demo"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                    new Claim(JwtClaimTypes.Role, "dispatcher"),
                }
            },
            new TestUser
            {
                SubjectId = "2",
                Username = "sam.okafor",
                Password = "DispatcherDemo!1",
                Claims =
                {
                    new Claim(JwtClaimTypes.Name, "Sam Okafor"),
                    new Claim(JwtClaimTypes.GivenName, "Sam"),
                    new Claim(JwtClaimTypes.FamilyName, "Okafor"),
                    new Claim(JwtClaimTypes.Email, "sam.okafor@gridpulse.demo"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                    new Claim(JwtClaimTypes.Role, "dispatcher"),
                }
            },
            new TestUser
            {
                SubjectId = "3",
                Username = "taylor.nguyen",
                Password = "DispatcherDemo!1",
                Claims =
                {
                    new Claim(JwtClaimTypes.Name, "Taylor Nguyen"),
                    new Claim(JwtClaimTypes.GivenName, "Taylor"),
                    new Claim(JwtClaimTypes.FamilyName, "Nguyen"),
                    new Claim(JwtClaimTypes.Email, "taylor.nguyen@gridpulse.demo"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                    new Claim(JwtClaimTypes.Role, "dispatcher"),
                }
            },
            new TestUser
            {
                SubjectId = "4",
                Username = "morgan.reyes",
                Password = "DispatcherDemo!1",
                Claims =
                {
                    new Claim(JwtClaimTypes.Name, "Morgan Reyes"),
                    new Claim(JwtClaimTypes.GivenName, "Morgan"),
                    new Claim(JwtClaimTypes.FamilyName, "Reyes"),
                    new Claim(JwtClaimTypes.Email, "morgan.reyes@gridpulse.demo"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                    new Claim(JwtClaimTypes.Role, "dispatcher"),
                }
            },
            new TestUser
            {
                SubjectId = "5",
                Username = "admin",
                Password = "IdentityAdmin!1",
                Claims =
                {
                    new Claim(JwtClaimTypes.Name, "Identity Admin"),
                    new Claim(JwtClaimTypes.Email, "admin@gridpulse.demo"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                }
            },
            new TestUser
            {
                SubjectId = "6",
                Username = "casey.brennan",
                Password = "ConsoleAdminDemo!1",
                Claims =
                {
                    new Claim(JwtClaimTypes.Name, "Casey Brennan"),
                    new Claim(JwtClaimTypes.GivenName, "Casey"),
                    new Claim(JwtClaimTypes.FamilyName, "Brennan"),
                    new Claim(JwtClaimTypes.Email, "casey.brennan@gridpulse.demo"),
                    new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                    new Claim(JwtClaimTypes.Role, "admin"),
                }
            }
        };
}
