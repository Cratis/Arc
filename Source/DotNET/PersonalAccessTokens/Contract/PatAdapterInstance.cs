// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.PersonalAccessTokens.Contract;

/// <summary>An isolated adapter under test. The harness owns its lifecycle.</summary>
/// <param name="Authority">The real persistence adapter.</param>
/// <param name="RecoveryEpoch">The real external recovery authority.</param>
/// <param name="Policy">The current product policy adapter.</param>
public sealed record PatAdapterInstance(IPatAuthority Authority, IPatRecoveryEpoch RecoveryEpoch, IPatPolicy Policy);
