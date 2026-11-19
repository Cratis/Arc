// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents the identity that opened an observable query connection, captured so that later control requests
/// naming that connection can be checked against it.
/// </summary>
/// <param name="IdentityId">The issuer- and tenant-qualified caller key, or an empty string when there is none.</param>
/// <param name="IsAuthenticated">Whether the caller's identity is authenticated.</param>
/// <remarks>
/// <para>
/// The Server-Sent Events transport splits one logical connection across several HTTP requests - a GET that holds
/// the stream open, and POSTs that subscribe and unsubscribe on it. The POSTs name the connection by identifier,
/// so without this the identifier alone would be enough to act on a connection somebody else opened.
/// </para>
/// <para>
/// This is a snapshot of values, holding no reference to the request it was taken from. It is captured once, inside
/// the GET handler while that context is still alive, and never refreshed - so the retained SSE context is never
/// read again afterwards, which is the property <c>b84ee1e6</c> established when a disposed context could surface
/// as a failure mid-subscribe.
/// </para>
/// <para>
/// Ownership compares the identity identifier, subject, or name, qualified by authentication type, issuer,
/// and tenant when present. An authenticated caller without any of these keys does not match another caller.
/// </para>
/// <para>
/// A connection opened by an unauthenticated caller carries no identity to bind to, so ownership cannot be
/// distinguished between anonymous callers. That is the behavior as it already stood, not a narrowing of it.
/// </para>
/// </remarks>
internal sealed record ObservableQueryCaller(string IdentityId, bool IsAuthenticated)
{
    /// <summary>
    /// Captures the caller of a request.
    /// </summary>
    /// <param name="context">The <see cref="IHttpRequestContext"/> to capture from.</param>
    /// <returns>The <see cref="ObservableQueryCaller"/> describing the request's caller.</returns>
    public static ObservableQueryCaller CapturedFrom(IHttpRequestContext context)
    {
        var principal = VerifiedCallerPrincipal.PrincipalFor(context.Items, context.User);
        var identity = principal?.Identities.OfType<VerifiedCallerIdentity>().FirstOrDefault()
            ?? principal?.Identity as ClaimsIdentity;
        return new ObservableQueryCaller(QualifiedKey(identity), identity?.IsAuthenticated ?? false);
    }

    /// <summary>
    /// Determines whether this caller is the same caller as <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The <see cref="ObservableQueryCaller"/> to compare against.</param>
    /// <returns>True when both describe the same caller, false otherwise.</returns>
    public bool IsSameCallerAs(ObservableQueryCaller other) =>
        (!IsAuthenticated || !string.IsNullOrEmpty(IdentityId)) &&
        IdentityId == other.IdentityId &&
        IsAuthenticated == other.IsAuthenticated;

    static string QualifiedKey(ClaimsIdentity? identity)
    {
        if (identity is null)
        {
            return string.Empty;
        }

        var claim = IdentityClaimFor(identity);
        var key = claim?.Value ?? identity.Name;
        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        return $"{KeyPart(identity.AuthenticationType ?? string.Empty)}{KeyPart(key)}{KeyPart(IssuerFor(identity, claim))}{KeyPart(identity.FindFirst(VerifiedCallerIdentity.TenantClaim)?.Value ?? string.Empty)}";
    }

    static Claim? IdentityClaimFor(ClaimsIdentity identity) =>
        NonEmptyClaimFor(identity, ClaimTypes.NameIdentifier)
        ?? NonEmptyClaimFor(identity, VerifiedCallerIdentity.SubjectClaim)
        ?? NonEmptyClaimFor(identity, identity.NameClaimType);

    static Claim? NonEmptyClaimFor(ClaimsIdentity identity, string type) =>
        identity.Claims.FirstOrDefault(_ => _.Type == type && !string.IsNullOrEmpty(_.Value));

    static string IssuerFor(ClaimsIdentity identity, Claim? claim) =>
        identity.FindFirst(VerifiedCallerIdentity.IssuerClaim)?.Value ?? claim?.Issuer ?? ClaimsIdentity.DefaultIssuer;

    static string KeyPart(string value) => $"{value.Length}:{value}";
}
