// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Chronicle.Identities;

using ChronicleIdentity = Cratis.Chronicle.Identities.Identity;

namespace Cratis.Arc.Chronicle.Identities;

/// <summary>
/// Preserves Chronicle identity resolution and adds Arc's authenticated off-request principal as a fallback.
/// </summary>
/// <param name="fallback">The identity provider selected by Chronicle.</param>
/// <param name="principals">The current Arc principal accessor.</param>
/// <param name="hasHttpRequest">Checks whether the host has an active HTTP request.</param>
/// <remarks>
/// Explicit identity tracking covers calls through this decorator only; calls directly to the wrapped provider
/// cannot distinguish an explicitly selected plain System identity from its default.
/// </remarks>
internal class ArcIdentityProvider(
    IIdentityProvider fallback,
    ICurrentPrincipalAccessor principals,
    Func<bool> hasHttpRequest) : IIdentityProvider
{
    readonly AsyncLocal<bool> _explicitIdentity = new();

    /// <inheritdoc/>
    public ChronicleIdentity GetCurrent()
    {
        var identity = fallback.GetCurrent();
        if (hasHttpRequest() || identity.Subject != ChronicleIdentity.System.Subject)
        {
            return identity;
        }

        var principal = principals.Current;
        if (principal?.Identity?.IsAuthenticated is not true)
        {
            return identity;
        }

        if (principals is CurrentPrincipalAccessor { HasOverride: true })
        {
            return FromPrincipal(principal) with { OnBehalfOf = identity.OnBehalfOf };
        }

        return !_explicitIdentity.Value && identity == ChronicleIdentity.System ? FromPrincipal(principal) : identity;
    }

    /// <inheritdoc/>
    public void SetCurrentIdentity(ChronicleIdentity identity)
    {
        fallback.SetCurrentIdentity(identity);
        _explicitIdentity.Value = true;
    }

    /// <inheritdoc/>
    public void ClearCurrentIdentity()
    {
        fallback.ClearCurrentIdentity();
        _explicitIdentity.Value = false;
    }

    /// <summary>
    /// Maps claims using Chronicle's ASP.NET identity rules, including its Identity.Name fallbacks.
    /// </summary>
    /// <param name="principal">The principal to map.</param>
    /// <returns>The corresponding Chronicle identity.</returns>
    internal static ChronicleIdentity FromPrincipal(ClaimsPrincipal principal)
    {
        var subject = principal.Claims.FirstOrDefault(_ => _.Type == "sub")?.Value ?? string.Empty;
        var name = principal.Claims.FirstOrDefault(_ => _.Type == "name")?.Value ?? principal.Identity?.Name ?? string.Empty;
        var userName = principal.Claims.FirstOrDefault(_ => _.Type == "preferred_username")?.Value ?? principal.Identity?.Name ?? string.Empty;
        return string.IsNullOrEmpty(subject) && string.IsNullOrEmpty(name) && string.IsNullOrEmpty(userName)
            ? ChronicleIdentity.NotSet
            : new ChronicleIdentity(subject, name, userName);
    }
}
