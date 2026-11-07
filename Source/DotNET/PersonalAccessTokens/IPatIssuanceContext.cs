// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Only already-verified request state may enter the issuance boundary.</summary>
internal interface IPatIssuanceContext
{
    /// <summary>Gets only the request's already-verified caller.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verified caller, or null.</returns>
    Task<VerifiedCaller?> GetCurrent(CancellationToken cancellationToken);
}
