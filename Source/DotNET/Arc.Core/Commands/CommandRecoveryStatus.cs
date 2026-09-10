// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Describes observed in-process recovery, not durable or atomic rollback.
/// </summary>
public enum CommandRecoveryStatus
{
    /// <summary>
    /// No started work requires recovery.
    /// </summary>
    NotNeeded = 0,

    /// <summary>
    /// Every required compensator returned successfully.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Recovery failed, was unavailable, or exhausted its cooperative budget.
    /// </summary>
    Incomplete = 2,

    /// <summary>
    /// Recovery was suppressed because business changes committed.
    /// </summary>
    Suppressed = 3,

    /// <summary>
    /// Recovery was not attempted because commitment is uncertain or mixed.
    /// </summary>
    Indeterminate = 4
}
