// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.PersonalAccessTokens;

namespace OAuthPatCallerAcceptance;

/// <summary>A fixed synthetic recovery epoch; not a production recovery authority.</summary>
public sealed class PatRecoveryEpoch : IPatRecoveryEpoch
{
    /// <inheritdoc/>
    public Task<ulong?> Read(CancellationToken cancellationToken) => Task.FromResult<ulong?>(1);
}
