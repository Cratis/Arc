// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity;

/// <summary>
/// Extension methods for <see cref="HttpRequest"/> for identity related functionality.
/// </summary>
public static class RequestExtensions
{
    /// <summary>
    /// Check if the request is a valid identity request.
    /// </summary>
    /// <param name="request"><see cref="HttpRequest"/> to check.</param>
    /// <returns>True if it is, false if not.</returns>
    public static bool IsValidIdentityRequest(this HttpRequest request) =>
        request.Headers.ContainsKey(MicrosoftIdentityPlatformHeaders.IdentityIdHeader) &&
        request.Headers.ContainsKey(MicrosoftIdentityPlatformHeaders.IdentityNameHeader) &&
        request.Headers.ContainsKey(MicrosoftIdentityPlatformHeaders.PrincipalHeader);

    /// <summary>
    /// Get the <see cref="ClientPrincipal"/> from the request.
    /// </summary>
    /// <param name="request"><see cref="HttpRequest"/> to get from.</param>
    /// <returns><see cref="ClientPrincipal"/> or null if it is invalid or doesn't exist.</returns>
    public static ClientPrincipal? GetClientPrincipal(this HttpRequest request)
    {
        var tokenAsString = request.Headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader].ToString();
        if (string.IsNullOrEmpty(tokenAsString))
        {
            return default;
        }
        var token = Convert.FromBase64String(tokenAsString);
        var jsonSerializerOptions = request.HttpContext.RequestServices
            .GetRequiredService<IOptions<ArcOptions>>()
            .Value
            .JsonSerializerOptions;
        return JsonSerializer.Deserialize<ClientPrincipal>(token, jsonSerializerOptions)!;
    }
}
