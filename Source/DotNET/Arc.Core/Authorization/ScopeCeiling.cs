// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Authorization;

/// <summary>
/// Enforces a verified caller's scope ceiling against the scopes an operation requires.
/// </summary>
/// <remarks>
/// The ceiling is read from the verified caller itself, never from claims, so a claims transformation cannot widen it.
/// </remarks>
internal static class ScopeCeiling
{
    /// <summary>
    /// Determines whether every verified caller on the principal holds every required scope.
    /// </summary>
    /// <param name="principal">The principal being authorized.</param>
    /// <param name="required">The scopes the operation requires.</param>
    /// <returns>True when nothing is required, when the principal carries no verified caller, or when every caller holds all required scopes.</returns>
    internal static bool Allows(ClaimsPrincipal? principal, IReadOnlyList<string> required)
    {
        if (required.Count == 0 || principal is null)
        {
            return true;
        }

        foreach (var identity in principal.Identities.OfType<VerifiedCallerIdentity>())
        {
            if (!required.All(scope => identity.Caller.Scopes.Contains(scope, StringComparer.Ordinal)))
            {
                return false;
            }
        }

        return true;
    }
}
