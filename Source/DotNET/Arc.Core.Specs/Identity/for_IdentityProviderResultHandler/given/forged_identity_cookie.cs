// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Arc.Identity.for_IdentityProvider.given;

/// <summary>
/// A <c>.cratis-identity</c> cookie a client wrote itself, claiming to be an administrator. Earlier versions of Arc
/// trusted this cookie ahead of the authenticated principal.
/// </summary>
public static class forged_identity_cookie
{
    public const string Name = ".cratis-identity";

    public static readonly IdentityProviderResult Claimed = new(
        new IdentityId("admin"),
        new IdentityName("Forged Administrator"),
        IsAuthenticated: true,
        IsAuthorized: true,
        Roles: ["admin"],
        Details: new { Department = "Forged" });

    public static IReadOnlyDictionary<string, string> AsCookies(ArcOptions options) => new Dictionary<string, string>
    {
        [Name] = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Claimed, options.JsonSerializerOptions)))
    };
}
