// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.given;

/// <summary>
/// Gives Castle an abstract invocation target for Chronicle's default <see cref="IEventSequence.SupportsIdempotentAppends"/>.
/// </summary>
public abstract class IdempotentEventLog : EventLogWithNamedTags, IEventLog
{
    public abstract bool SupportsIdempotentAppends { get; }

    public List<EventForEventSourceId[]> Attempts { get; } = [];
    public Dictionary<IdempotencyKey, EventForEventSourceId> Appended { get; } = [];

    public AppendManyResult Record(IEnumerable<EventForEventSourceId> events)
    {
        var batch = events.ToArray();
        Attempts.Add(batch);
        foreach (var envelope in batch)
        {
            if (envelope.IdempotencyKey is not { } key) continue;
            if (Appended.TryGetValue(key, out var existing) && existing != envelope)
            {
                return AppendManyResult.Failed(CorrelationId.NotSet, [new AppendError("IdempotencyKeyConflict")]);
            }
            Appended[key] = envelope;
        }
        return AppendManyResult.Success(CorrelationId.NotSet, [EventSequenceNumber.First]);
    }
}
