// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors;

internal static class RefusalEvents
{
    public static Task<Result<ReactorSideEffectFailure>> Append(ReactorContext context, IEventStore store, object value)
    {
        var events = value is IEnumerable<object> sequence ? sequence.ToArray() : [value];
        if (events.Length == 0 || !events.All(item => IsEvent(store, item)))
        {
            return Task.FromResult(Result.Failed(new ReactorSideEffectFailure([new AppendFailure([], false, ["A refusal branch must return registered events or null to acknowledge."], [])])));
        }

        return new MixedSideEffectsResultHandler().Handle(context, store, events);
    }

    static bool IsEvent(IEventStore store, object? value)
    {
        var @event = value is EventForEventSourceId envelope ? envelope.Event : value;

        return @event is not null and not EventForEventSourceId && store.EventTypes.HasFor(@event.GetType()) && !@event.GetType().IsCommand();
    }
}
