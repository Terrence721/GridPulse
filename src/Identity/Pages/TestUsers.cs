// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using IdentityModel;
using System.Security.Claims;
using Duende.IdentityServer.Test;

namespace GridPulse.Identity;

// Dev-only demo credentials for the Grid Operations console - not real
// users, no role claims (this slice has a single "dispatcher" persona,
// nothing authorizes on role yet).
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
                }
            }
        };
}
