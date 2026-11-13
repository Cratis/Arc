// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.PersonalAccessTokens;

namespace OAuthPatCallerAcceptance;

/// <summary>The sample's current policy, evaluated independently of the forwarded proof.</summary>
/// <param name="store">Current persisted person status.</param>
public sealed class PatPolicy(SampleIdentityStore store) : IPatPolicy
{
    /// <inheritdoc/>
    public Task<PatPolicyDecision> Evaluate(PatRecord record, bool issuing, CancellationToken cancellationToken) =>
        Task.FromResult(new PatPolicyDecision(store.PersonExists, TimeSpan.FromHours(1), 1));
}
