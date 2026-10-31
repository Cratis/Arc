// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Arc.Identity;

/// <summary>
/// Authenticates canonical v1 bytes and binds the decoded represented subject before reserving a nonce.
/// </summary>
internal static class ForwardedIdentityVerification
{
    /// <summary>
    /// Verifies MAC, freshness, identity binding, and process-local replay protection.
    /// </summary>
    /// <param name="request">The bounded canonical request.</param>
    /// <param name="envelope">The strictly parsed envelope.</param>
    /// <param name="configuration">The validated keys.</param>
    /// <param name="options">The host options.</param>
    /// <param name="now">The current UTC Unix seconds.</param>
    /// <param name="replays">The replay store.</param>
    /// <returns>The safe represented principal, or null if rejected.</returns>
    internal static ClientPrincipal? Verify(ForwardedIdentityRequest request, ForwardedIdentityEnvelope envelope, ForwardedIdentityConfiguration configuration, ArcOptions options, long now, ForwardedIdentityReplayCache replays)
    {
        if (envelope.Timestamp < now - 60 || envelope.Timestamp > now + 30 || !configuration.Keys.TryGetValue(envelope.KeyId, out var key))
        {
            return null;
        }
        using var hmac = new HMACSHA256(key);
        if (!CryptographicOperations.FixedTimeEquals(hmac.ComputeHash(request.CanonicalBytes), envelope.Mac))
        {
            return null;
        }
        var principal = ReadPrincipal(request.Headers[MicrosoftIdentityPlatformHeaders.PrincipalHeader], options.JsonSerializerOptions);
        var subject = ForwardedHeaderValue.Decode(request.Headers[MicrosoftIdentityPlatformHeaders.IdentityIdHeader]);
        if (principal is null || string.IsNullOrWhiteSpace(subject) || !string.Equals(principal.UserId, subject, StringComparison.Ordinal))
        {
            return null;
        }
        return replays.TryReserve(options.ForwardedIdentity.Audience, envelope, now) ? principal : null;
    }

    /// <summary>
    /// Decodes a structurally valid principal with strict UTF-8 and nonnull claims and roles.
    /// </summary>
    /// <param name="header">The base64 principal header.</param>
    /// <param name="options">The configured JSON options.</param>
    /// <returns>The principal, or null if invalid.</returns>
    internal static ClientPrincipal? ReadPrincipal(string header, JsonSerializerOptions options)
    {
        try
        {
            var bytes = Convert.FromBase64String(header);
            var json = new UTF8Encoding(false, true).GetString(bytes);
            var principal = JsonSerializer.Deserialize(json, options.ResolveTypeInfo<ClientPrincipal>());
            return principal is not null && !string.IsNullOrWhiteSpace(principal.UserId) && principal.UserDetails is not null &&
                principal.Claims is not null && principal.UserRoles is not null &&
                principal.Claims.All(claim => !string.IsNullOrEmpty(claim?.typ) && claim.val is not null) &&
                principal.UserRoles.All(role => role is not null) ? principal : null;
        }
        catch (Exception exception) when (exception is FormatException or DecoderFallbackException or JsonException or NotSupportedException)
        {
            return null;
        }
    }
}
