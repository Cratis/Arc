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

    /// <summary>The request item holding the admitted caller handed to the authentication handler.</summary>
    internal const string CallerItemKey = "Cratis.Arc.VerifiedCaller";

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
    internal static ClaimsPrincipal? From(IHttpRequestContext request) =>
        request.Items.TryGetValue(ItemKey, out var value) ? value as ClaimsPrincipal : null;

    /// <summary>
    /// Determines whether a principal carries the identity of exactly this verified caller.
    /// </summary>
    /// <param name="principal">The principal.</param>
    /// <param name="caller">The verified caller.</param>
    /// <returns>True when any identity of the principal was created for the same caller instance.</returns>
    internal static bool Carries(ClaimsPrincipal? principal, VerifiedCaller caller) =>
        principal?.Identities.OfType<VerifiedCallerIdentity>().Any(identity => ReferenceEquals(identity.Caller, caller)) == true;
}
