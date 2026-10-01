// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Testing.Commands;

/// <summary>
/// The exception that is thrown when Chronicle refuses the shape of one or more protected decision reads.
/// </summary>
/// <param name="refusals">The refused decision reads.</param>
public class DecisionReadsAreRefused(IReadOnlyList<RefusedDecisionRead> refusals)
    : Exception(
        "Chronicle refuses to guard these protected decision reads, so the commands would fail when executed:" + Environment.NewLine +
        string.Join(Environment.NewLine, refusals.Select(_ => $"- {_.CommandType.FullName}: DecisionRead<{_.ReadModelType.FullName}> is refused ({_.Reason})")) + Environment.NewLine +
        "A decision read needs exactly one flat projection on the event log, keyed by the event source id, without children, " +
        "nested objects, joins or reducers. Read a dedicated read model of that shape instead, or keep the command's " +
        "existing concurrency handling for that state.")
{
    /// <summary>
    /// Gets the refused decision reads.
    /// </summary>
    public IReadOnlyList<RefusedDecisionRead> Refusals { get; } = refusals;
}
