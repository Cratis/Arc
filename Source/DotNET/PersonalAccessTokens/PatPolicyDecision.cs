// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens;

/// <summary>Current policy evaluation, never a cached authorization.</summary>
/// <param name="Permitted">Whether current policy permits the credential.</param>
/// <param name="MaximumLifetime">Current organization lifetime ceiling.</param>
/// <param name="PolicyEpoch">Current policy epoch.</param>
public sealed record PatPolicyDecision(bool Permitted, TimeSpan MaximumLifetime, ulong? PolicyEpoch);
