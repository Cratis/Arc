// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>No permissive default policy.</summary>
internal sealed class UnavailablePatPolicy : IPatPolicy
{
    /// <inheritdoc/>
    public Task<PatPolicyDecision> Evaluate(PatRecord record, bool issuing, CancellationToken cancellationToken) => Task.FromResult(new PatPolicyDecision(false, TimeSpan.Zero, null));
}
