// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.Identity;

/// <summary>
/// Checks that a verified on-behalf assertion belongs to the request it arrived with.
/// </summary>
internal static class OnBehalfOfForwardBinding
{
    /// <summary>
    /// Checks the assertion against the request.
    /// </summary>
    /// <param name="assertion">The verified <see cref="OnBehalfOfAssertion"/>.</param>
    /// <param name="forward">The <see cref="OnBehalfOfForward"/> values of the request.</param>
    /// <param name="represented">The represented user's <see cref="ClientPrincipal"/>, read from the principal header.</param>
    /// <returns>The failure reason, or <see langword="null"/> when the assertion belongs to the request.</returns>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>principal_hash</c> must be the SHA-256 of the exact principal header, <c>sub</c> must be its <c>userId</c>,
    /// and the principal id header, decoded from AuthProxy's RFC 8187 transport form when it carries one, must be that
    /// same <c>userId</c>.</item>
    /// <item><c>actor_hash</c> must be the SHA-256 of the exact actor header.</item>
    /// <item>When the request carries the configured tenant header, <c>tenant</c> must equal it.</item>
    /// <item>When present, <c>htm</c> must equal the request method and <c>htu</c> the request path - path base and path,
    /// without scheme, host or query - compared after <see cref="OnBehalfOfRequestPath.Normalize"/>.</item>
    /// </list>
    /// </remarks>
    internal static string? Check(OnBehalfOfAssertion assertion, OnBehalfOfForward forward, ClientPrincipal represented)
    {
        if (!HashMatches(assertion.PrincipalHash, forward.Principal) ||
            !string.Equals(assertion.Subject, represented.UserId, StringComparison.Ordinal) ||
            !string.Equals(ForwardedHeaderValue.Decode(forward.PrincipalId), represented.UserId, StringComparison.Ordinal))
        {
            return OnBehalfOfFailures.PrincipalMismatch;
        }

        if (!HashMatches(assertion.ActorHash, forward.Actor ?? string.Empty))
        {
            return OnBehalfOfFailures.ActorMismatch;
        }

        if (!string.IsNullOrEmpty(forward.Tenant) && !string.Equals(assertion.Tenant, forward.Tenant, StringComparison.Ordinal))
        {
            return OnBehalfOfFailures.TenantMismatch;
        }

        if (assertion.Method is not null && !string.Equals(assertion.Method, forward.Method, StringComparison.Ordinal))
        {
            return OnBehalfOfFailures.MethodMismatch;
        }

        return assertion.Target is null || TargetMatches(assertion.Target, forward) ? null : OnBehalfOfFailures.TargetMismatch;
    }

    static bool HashMatches(string expected, string headerValue) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(OnBehalfOfBase64Url.Sha256Of(headerValue)));

    static bool TargetMatches(string target, OnBehalfOfForward forward) =>
        target.StartsWith('/') &&
        OnBehalfOfRequestPath.Normalize(target) is { } signed &&
        OnBehalfOfRequestPath.Normalize(forward.Path) is { } received &&
        string.Equals(signed, received, StringComparison.Ordinal);
}
