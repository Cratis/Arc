// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.Identity.ForwardedIdentityV2;

/// <summary>Finds a persisted person by verified account key, never by claims projection.</summary>
internal interface ICallerPersonDirectory
{
    /// <summary>Finds the persisted person linked to an account.</summary>
    /// <param name="accountKey">The verified account key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A linked person or an explicit not-found result.</returns>
    Task<CallerPersonFound> Find(CallerAccountKey accountKey, CancellationToken cancellationToken);
}
