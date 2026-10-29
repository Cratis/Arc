// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors;

/// <summary>
/// Validates branch productions before appending through the ordinary reactor event path.
/// </summary>
internal static class RefusalEvents
{
    /// <summary>
    /// Appends a branch's registered events with the reactor's resolved routing.
    /// </summary>
    /// <param name="context">The reactor context.</param>
    /// <param name="store">The current event store.</param>
    /// <param name="value">The branch production.</param>
    /// <param name="commandIndex">The command's zero-based position in the returned commands.</param>
    /// <returns>The append outcome or a failure for an invalid production.</returns>
    public static Task<Result<ReactorSideEffectFailure>> Append(ReactorContext context, IEventStore store, object value, int commandIndex)
    {
        var events = value is IEnumerable<object> sequence ? sequence.ToArray() : [value];
        if (events.Length == 0 || !events.All(item => IsEvent(store, item)))
        {
            return Task.FromResult(Result.Failed(new ReactorSideEffectFailure([new AppendFailure([], false, ["A refusal branch must return registered events or null to acknowledge."], [])])));
        }

        if (context.Delivery is not null && store.EventLog.SupportsIdempotentAppends)
        {
            events = events.Select((item, eventIndex) => WithKey(context, item, commandIndex, eventIndex)).ToArray();
        }

        return new MixedSideEffectsResultHandler().Handle(context, store, events);
    }

    static EventForEventSourceId WithKey(ReactorContext context, object value, int commandIndex, int eventIndex)
    {
        var envelope = value as EventForEventSourceId ?? new EventForEventSourceId(context.GetEventSourceId(), value)
        {
            EventStreamType = context.GetEventStreamType() ?? EventStreamType.All,
            EventStreamId = context.GetEventStreamId() ?? EventStreamId.Default,
            EventSourceType = context.GetEventSourceType() ?? EventSourceType.Default,
            Subject = context.GetSubject()
        };
        return envelope.IdempotencyKey is not null ? envelope : envelope with
        {
            IdempotencyKey = context.Delivery!.IdempotencyKeyFor(string.Create(CultureInfo.InvariantCulture, $"arc:refusal:{commandIndex}:{eventIndex}"))
        };
    }

    static bool IsEvent(IEventStore store, object? value)
    {
        var @event = value is EventForEventSourceId envelope ? envelope.Event : value;

        return @event is not null and not EventForEventSourceId && store.EventTypes.HasFor(@event.GetType()) && !@event.GetType().IsCommand();
    }
}
