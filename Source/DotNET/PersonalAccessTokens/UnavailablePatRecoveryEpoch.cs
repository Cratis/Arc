// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Fail-closed until an internal recovery authority is installed.</summary>
internal sealed class UnavailablePatRecoveryEpoch : IPatRecoveryEpoch
{
    /// <inheritdoc/>
    public Task<ulong?> Read(CancellationToken cancellationToken) => Task.FromResult<ulong?>(null);
}
