// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens.for_PersonalAccessTokens.given;

/// <summary>Hand-written issuance context that serves the caller supplied by the owning specification.</summary>
/// <param name="current">Gets the current verified caller.</param>
internal sealed class FakeIssuanceContext(Func<VerifiedCaller?> current) : IPatIssuanceContext
{
    /// <inheritdoc/>
    public Task<VerifiedCaller?> GetCurrent(CancellationToken cancellationToken) => Task.FromResult(current());
}
