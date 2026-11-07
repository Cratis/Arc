// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>
/// Trusted-host recovery contract, not a sandbox. Null or unavailable always refuses.
/// Evidence must live outside the store's restore domain; restore must fence old credentials.
/// Missing evidence must never be recreated by reading it.
/// </summary>
public interface IPatRecoveryEpoch
{
    /// <summary>
    /// Reads current authoritative recovery state with linearizable reads ordered against revocation and recovery-epoch advancement.
    /// </summary>
    /// <remarks>
    /// Bypass positive caches. Adapters that cannot guarantee this ordering must fail closed and return null or fail the read.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The epoch, or null on uncertainty.</returns>
    Task<ulong?> Read(CancellationToken cancellationToken);
}
