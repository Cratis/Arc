// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Streams;

/// <summary>
/// Represents tail-first reads from the event log for routed command decisions.
/// </summary>
/// <param name="eventLog">The event log belonging to the active event store namespace.</param>
/// <param name="commandContext">The current command context.</param>
public class StreamReads(IEventLog eventLog, ICommandContextAccessor commandContext) : IStreamReads
{
    /// <inheritdoc/>
    public Task<StreamDecision> Stream(EventSourceId id, EventRoute route, CancellationToken ct = default) => Read(id, route, false, ct);

    /// <inheritdoc/>
    public Task<StreamDecision> StreamType(EventSourceId id, EventRoute route, CancellationToken ct = default) => Read(id, route, true, ct);

    /// <inheritdoc/>
    public Task<StreamDecision> ForCommand(CancellationToken ct = default) => Stream(commandContext.Current.GetEventSourceId(), commandContext.Current.GetEventRoute(), ct);

    async Task<StreamDecision> Read(EventSourceId id, EventRoute route, bool wholeType, CancellationToken ct)
    {
        if (!id.IsSpecified || route.EventStreamType.IsAll || string.IsNullOrWhiteSpace(route.EventStreamType.Value) ||
            route.EventStreamId.IsDefault || string.IsNullOrWhiteSpace(route.EventStreamId.Value))
        {
            throw new StreamDecisionRequiresSpecificRoute(id, route);
        }

        ct.ThrowIfCancellationRequested();
        var sourceType = route.EventSourceType == EventSourceType.Default || route.EventSourceType == EventSourceType.Unspecified ? null : route.EventSourceType;
        var streamId = wholeType ? null : route.EventStreamId;
        var unit = CommandTransaction.Current as UnitOfWork;
        var enforcing = unit?.IsEnforcingReadProtection == true;
        if (enforcing && unit!.IsCompleted)
        {
            throw new ReadAfterProtectedCommit("Stream decision");
        }
        using var allowance = enforcing ? unit!.AllowReads(ReadAllowance.Contributed, "Stream decision") : null;
        var tail = await eventLog.GetTailSequenceNumber(id, sourceType, route.EventStreamType, streamId).WaitAsync(ct);
        var events = await eventLog.GetForEventSourceIdAndEventTypes(id, [], route.EventStreamType, streamId, sourceType).WaitAsync(ct);
        IImmutableList<AppendedEvent> captured = tail.IsUnavailable ? [] : events.Where(@event => @event.Context.SequenceNumber <= tail).ToImmutableList();
        var scope = new Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScopeBuilder()
            .WithEventSourceId(id)
            .WithEventStreamType(route.EventStreamType);
        if (sourceType is not null)
        {
            scope.WithEventSourceType(sourceType);
        }
        if (!wholeType)
        {
            scope.WithEventStreamId(route.EventStreamId);
        }
        if (tail.IsUnavailable)
        {
            scope.ExpectingNoMatchingEvent();
        }
        else
        {
            scope.WithSequenceNumber(tail);
        }

        var built = scope.Build();
        if (enforcing)
        {
            var store = commandContext.Current.ServiceProvider!.GetRequiredService<IEventStore>();
            unit!.EnrolContributedRead(new(ConcurrencyScopeOrigin.Explicit, id, built, store.Name, store.Namespace, eventLog.Id, "Stream decision"));
        }

        return new(id, route, tail, captured, built);
    }
}
