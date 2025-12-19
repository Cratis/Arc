// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace System.Security.Principal;

/// <summary>
/// Extension methods for <see cref="IIdentity"/>.
/// </summary>
public static class IdentityExtensions
{
    /// <summary>
    /// Get the user id as a <see cref="Guid"/> from the <see cref="IIdentity"/>.
    /// </summary>
    /// <param name="identity"><see cref="IIdentity"/> to get from.</param>
    /// <returns>The Guid.</returns>
    /// <remarks>
    /// If there is no user id, <see cref="Guid.Empty"/> will be returned.
    /// </remarks>
    public static Guid GetUserIdAsGuid(this IIdentity identity)
    {
        if (identity is ClaimsIdentity claimsIdentity)
        {
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userId ?? Guid.Empty.ToString());
        }

        return Guid.Empty;
    }
}