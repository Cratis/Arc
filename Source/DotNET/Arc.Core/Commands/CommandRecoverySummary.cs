// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Sanitized recovery observations suitable for transport. No business descriptors or exception details are included.
/// </summary>
/// <param name="CommitDisposition">Observed coordinated commitment.</param>
/// <param name="Status">Observed recovery status.</param>
/// <param name="StartedCount">Invocations entered.</param>
/// <param name="CompletedCount">Execute methods that returned.</param>
/// <param name="CompensatedCount">Compensate methods that returned.</param>
/// <param name="FailedCompensationCount">Compensate methods that threw.</param>
/// <param name="UncompensatedCount">Started invocations needing recovery that did not complete compensation.</param>
public sealed record CommandRecoverySummary(
    CommandCommitDisposition CommitDisposition,
    CommandRecoveryStatus Status,
    int StartedCount,
    int CompletedCount,
    int CompensatedCount,
    int FailedCompensationCount,
    int UncompensatedCount);
