// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// Describes how an <see cref="IOccurrenceCommandExecutor"/> execution settled.
/// </summary>
public enum OccurrenceCommandDisposition
{
    /// <summary>
    /// This execution wrote the durable effect receipt: the effect is settled and must not be run again.
    /// </summary>
    Recorded = 0,

    /// <summary>
    /// A durable receipt already existed for the effect key. The stored outcome is returned and nothing was run again.
    /// </summary>
    AlreadyRecorded = 1,

    /// <summary>
    /// The attempt failed for a reason a later attempt can recover from. No receipt was written and nothing was committed.
    /// </summary>
    Failed = 2
}
