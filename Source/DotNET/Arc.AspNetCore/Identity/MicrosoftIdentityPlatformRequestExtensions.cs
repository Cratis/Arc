// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Identity;

/// <summary>
/// Extensions for <see cref="HttpRequest"/> for Microsoft Identity Platform.
/// </summary>
public static class MicrosoftIdentityPlatformRequestExtensions
{
    /// <summary>
    /// Convert the claims into standard .NET <see cref="Claim"/> from the client principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClientPrincipal"/> to convert from.</param>
    /// <returns>Converted claims.</returns>
    public static IImmutableList<Claim> GetClaims(this ClientPrincipal principal)
    {
        var claims = new List<Claim>();
        foreach (var claim in principal.Claims)
        {
            claims.Add(new Claim(claim.typ, claim.val));
        }

        return claims.ToImmutableList();
    }

    /// <summary>
    /// Get the claims from the request in a raw key/value form..
    /// </summary>
    /// <param name="principal">The <see cref="ClientPrincipal"/> to convert from.</param>
    /// <returns>Converted claims.</returns>
    public static IEnumerable<KeyValuePair<string, string>> GetClaimsRaw(this ClientPrincipal principal)
    {
        return principal.Claims.Select(claim => new KeyValuePair<string, string>(claim.typ, claim.val));
    }

    /// <summary>
    /// Get the claims from the request.
    /// </summary>
    /// <param name="request"><see cref="HttpRequest"/> to get from.</param>
    /// <returns>Collection of key / value with claims.</returns>
    public static IImmutableList<Claim> GetClaims(this HttpRequest request)
    {
        var tokenAsString = request.Headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader].ToString();
        if (string.IsNullOrEmpty(tokenAsString))
        {
            return ImmutableList<Claim>.Empty;
        }
        var token = Convert.FromBase64String(tokenAsString);

        var tokenAsJson = JsonNode.Parse(token) as JsonObject;
        var claims = new List<Claim>();
        if (tokenAsJson is not null && tokenAsJson.TryGetPropertyValue("claims", out var claimsArray) && claimsArray is JsonArray claimsAsArray)
        {
            foreach (var claim in claimsAsArray.Cast<JsonObject>())
            {
                if (claim.TryGetPropertyValue("typ", out var type) &&
                    claim.TryGetPropertyValue("val", out var value))
                {
                    claims.Add(new Claim(type!.ToString(), value!.ToString()));
                }
            }
        }

        return claims.ToImmutableList();
    }
}