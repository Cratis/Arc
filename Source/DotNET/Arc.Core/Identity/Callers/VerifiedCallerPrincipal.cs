// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// Creates and reads the principal that represents a verified caller on a request.
/// </summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
internal static class VerifiedCallerPrincipal
{
    /// <summary>The request item holding the verified principal Arc authorizes.</summary>
    internal const string ItemKey = "Cratis.Arc.VerifiedCallerPrincipal";

    /// <summary>
    /// Creates the principal for a verified caller.
    /// </summary>
    /// <param name="caller">The verified caller.</param>
    /// <returns>A principal with exactly one <see cref="VerifiedCallerIdentity"/>.</returns>
    internal static ClaimsPrincipal Create(VerifiedCaller caller) => new(new VerifiedCallerIdentity(caller));

    /// <summary>
    /// Reads the verified principal a request carries.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>The verified principal, or null when the request has none.</returns>
    internal static ClaimsPrincipal? From(IHttpRequestContext request) => From(request.Items);

    /// <summary>
    /// Reads the verified principal from a request's item bag.
    /// </summary>
    /// <param name="items">The request items.</param>
    /// <returns>The verified principal, or null when the items hold none.</returns>
    internal static ClaimsPrincipal? From(IDictionary<object, object?> items) =>
        items.TryGetValue(ItemKey, out var value) ? value as ClaimsPrincipal : null;

    /// <summary>
    /// Gets the principal admitted for an HTTP request.
    /// </summary>
    /// <param name="request">The request carrying the admitted principal.</param>
    /// <returns>The verified principal, or the request user for legacy hosts.</returns>
    internal static ClaimsPrincipal PrincipalFor(IHttpRequestContext request) => PrincipalFor(request.Items, request.User)!;

    /// <summary>
    /// The single source of the principal Arc authorizes against: the gate's verified principal when the request has one,
    /// otherwise the supplied fallback. Hosts replace the request user after the gate runs, so the user alone is never trusted
    /// once a verified principal exists.
    /// </summary>
    /// <param name="items">The request items.</param>
    /// <param name="fallback">The principal to use when the request carries no verified principal.</param>
    /// <returns>The principal to authorize.</returns>
    internal static ClaimsPrincipal? PrincipalFor(IDictionary<object, object?> items, ClaimsPrincipal? fallback) =>
        From(items) ?? fallback;

    /// <summary>
    /// Determines whether a principal carries the identity of exactly this verified caller.
    /// </summary>
    /// <param name="principal">The principal.</param>
    /// <param name="caller">The verified caller.</param>
    /// <returns>True when any identity of the principal was created for the same caller instance.</returns>
    internal static bool Carries(ClaimsPrincipal? principal, VerifiedCaller caller) =>
        principal?.Identities.OfType<VerifiedCallerIdentity>().Any(identity => ReferenceEquals(identity.Caller, caller)) == true;

    /// <summary>
    /// Determines whether a principal carries another authenticated identity whose subject is missing or differs from the caller's.
    /// </summary>
    /// <param name="principal">The principal.</param>
    /// <param name="caller">The verified caller.</param>
    /// <returns>True when such an identity is present, so the principal must be refused rather than merged.</returns>
    internal static bool CarriesAnotherSubject(ClaimsPrincipal? principal, VerifiedCaller caller)
    {
        var subject = VerifiedCallerIdentity.SubjectOf(caller);
        return principal?.Identities.Any(identity =>
            identity.IsAuthenticated &&
            !(identity is VerifiedCallerIdentity verified && ReferenceEquals(verified.Caller, caller)) &&
            !string.Equals(
                (identity.FindFirst(VerifiedCallerIdentity.SubjectClaim) ?? identity.FindFirst(ClaimTypes.NameIdentifier))?.Value,
                subject,
                StringComparison.Ordinal)) == true;
    }
}
