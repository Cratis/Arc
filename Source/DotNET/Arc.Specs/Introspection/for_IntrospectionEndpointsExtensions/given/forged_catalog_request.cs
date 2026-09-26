// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.Identity;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions.given;

internal static class forged_catalog_request
{
    internal static HttpRequestMessage Create(string path)
    {
        var principal = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(new
        {
            userId = "forged",
            userDetails = "attacker@example.com",
            userRoles = new[] { "Administrator" },
            claims = Array.Empty<object>()
        }));
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(MicrosoftIdentityPlatformHeaders.IdentityIdHeader, "forged");
        request.Headers.Add(MicrosoftIdentityPlatformHeaders.IdentityNameHeader, "attacker");
        request.Headers.Add(MicrosoftIdentityPlatformHeaders.PrincipalHeader, principal);
        return request;
    }
}
