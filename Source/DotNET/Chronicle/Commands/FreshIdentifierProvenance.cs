// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Keeps generated-target provenance with the unit of work, including nested invocations.
/// </summary>
internal sealed class FreshIdentifierProvenance
{
    readonly HashSet<(EventSequenceId Sequence, EventSourceId Source)> _targets = [];

    /// <summary>
    /// Records a guarded target enrolled in this unit.
    /// </summary>
    /// <param name="sequence">The sequence.</param>
    /// <param name="source">The source.</param>
    internal void Add(EventSequenceId sequence, EventSourceId source) => _targets.Add((sequence, source));

    /// <summary>
    /// Checks a target already enrolled in the unit's sequence.
    /// </summary>
    /// <param name="sequence">The destination sequence.</param>
    /// <param name="source">The destination source.</param>
    /// <returns>Whether the target is guarded.</returns>
    internal bool Contains(EventSequenceId sequence, EventSourceId source) => _targets.Contains((sequence, source));

    /// <summary>
    /// Classifies a violation in this unit, which Chronicle bounds to one sequence.
    /// </summary>
    /// <param name="violation">The reported violation.</param>
    /// <returns>Whether the violated BeforeFirst guard had generated provenance.</returns>
    internal bool Owns(ConcurrencyViolation violation) => violation.ExpectedEventSequenceNumber.IsBeforeFirst &&
        _targets.Any(target => target.Source == violation.EventSourceId);
}
