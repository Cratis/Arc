// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Bridges issuance only from the verified-caller accessor.</summary>
/// <param name="callers">Request-scoped verified callers.</param>
internal sealed class VerifiedPatIssuanceContext(IVerifiedCallerAccessor callers) : IPatIssuanceContext
{
    /// <inheritdoc/>
    public async Task<VerifiedCaller?> GetCurrent(CancellationToken cancellationToken) => (await callers.GetCurrent()).Caller;
}
