// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Current authoritative issuance epoch for this installation. Null or unavailable always refuses.</summary>
internal interface IPatRecoveryEpoch
{
    /// <summary>Reads current authoritative recovery state without a positive cache.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The epoch, or null on uncertainty.</returns>
    Task<ulong?> Read(CancellationToken cancellationToken);
}
