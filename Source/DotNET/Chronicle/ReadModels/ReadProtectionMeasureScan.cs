// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// An immutable, bounded metadata scan. The event log holds its Chronicle target, not a request service provider.
/// </summary>
/// <param name="Command">The measured command.</param>
/// <param name="Log">The exact target log.</param>
/// <param name="Boundary">G0.</param>
/// <param name="Reads">The passively observed predicates.</param>
/// <param name="OwnPositions">Exact positions observed on this invocation's append flow.</param>
/// <param name="Limit">Maximum metadata locators to request.</param>
/// <param name="Timeout">The scan's total deadline.</param>
/// <param name="Logger">The structured telemetry sink.</param>
internal sealed record ReadProtectionMeasureScan(Type Command, IEventSequence Log, EventSequenceNumber Boundary, IReadOnlyList<MeasuredRead> Reads, IReadOnlyList<EventSequenceNumber> OwnPositions, int Limit, TimeSpan Timeout, ILogger<ReadProtectionMeasurement>? Logger)
{
    /// <summary>
    /// Scans off the request path, excluding own positions one by one, never by correlation or a tail-minus-count range.
    /// </summary>
    /// <param name="cancellationToken">The worker's shutdown deadline.</param>
    /// <returns>The scan task; all failures become unavailable telemetry.</returns>
    internal async Task Run(CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(Timeout);
        try
        {
            var tail = EventSequenceNumber.BeforeFirst;
            foreach (var read in Reads)
            {
                var filtered = await Log.GetTailSequenceNumber(read.Source == EventSourceId.Unspecified ? null : read.Source, read.SourceType, read.StreamType, read.StreamId, read.Types).WaitAsync(deadline.Token);
                if (filtered.IsActualValue && (!tail.IsActualValue || filtered > tail)) tail = filtered;
            }
            if (!tail.IsActualValue || (Boundary.IsActualValue && tail <= Boundary)) return;
            var first = Boundary.IsActualValue ? Boundary.Value + 1 : EventSequenceNumber.First.Value;
            var available = tail.Value - first + 1;
            var count = (int)Math.Min(available, (ulong)Math.Clamp(Limit, 1, EventMetadata.MaxLocators));
            var locators = Enumerable.Range(0, count).Select(_ => (EventSequenceNumber)(first + (ulong)_)).ToArray();

            // GetFromSequenceNumber has no upper bound. The public metadata batch API bounds server AND client
            // volume, includes all sources, and provides every dimension needed for these predicates.
            var metadata = await Log.GetMetadataAt(locators, deadline.Token).WaitAsync(deadline.Token);
            var own = OwnPositions.ToHashSet();
            foreach (var item in metadata.Values.Where(_ => _.SequenceNumber.Value >= first && _.SequenceNumber <= tail && !own.Contains(_.SequenceNumber)))
            {
                foreach (var read in Reads.Where(_ => _.Matches(item)))
                {
                    ReadProtectionTelemetry.Record(Command, "matching_events_since_start", read.Model, $"Foreign event {item.EventTypeId} at {item.SequenceNumber} would have conflicted; On might have returned 400", Logger);
                }
            }
            if (available > (ulong)count) ReadProtectionTelemetry.Record(Command, "measurement_unavailable", reason: "Scan volume limit; report is incomplete", logger: Logger);
        }
        catch (Exception failure)
        {
            ReadProtectionTelemetry.Record(Command, "measurement_unavailable", reason: failure.GetType().Name, logger: Logger);
        }
    }
}
