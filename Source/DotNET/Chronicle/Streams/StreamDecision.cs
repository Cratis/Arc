// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Streams;

/// <summary>
/// Represents stream facts read at a captured tail and the scope protecting an append decided from them.
/// </summary>
public sealed class StreamDecision
{
    /// <summary>
    /// Initializes a decision captured by the stream reader.
    /// </summary>
    /// <param name="eventSourceId">The event source id.</param>
    /// <param name="route">The append route.</param>
    /// <param name="tail">The captured tail.</param>
    /// <param name="events">The events at the tail.</param>
    /// <param name="concurrencyScope">The exact guard.</param>
    internal StreamDecision(EventSourceId eventSourceId, EventRoute route, EventSequenceNumber tail, IImmutableList<AppendedEvent> events, ConcurrencyScope concurrencyScope)
    {
        EventSourceId = eventSourceId;
        Route = route;
        Tail = tail;
        Events = events;
        ConcurrencyScope = concurrencyScope;
    }

    /// <summary>
    /// Gets the source read and targeted by appended facts.
    /// </summary>
    public EventSourceId EventSourceId { get; }

    /// <summary>
    /// Gets the route used by appended facts.
    /// </summary>
    public EventRoute Route { get; }

    /// <summary>
    /// Gets the tail captured before the events were read.
    /// </summary>
    public EventSequenceNumber Tail { get; }

    /// <summary>
    /// Gets whether no matching event existed at the captured tail.
    /// </summary>
    public bool IsEmpty => Tail.IsUnavailable;

    /// <summary>
    /// Gets the facts at or before the captured tail.
    /// </summary>
    public IImmutableList<AppendedEvent> Events { get; }

    /// <summary>
    /// Gets the exact scope protecting a nonempty append from this decision.
    /// </summary>
    public ConcurrencyScope ConcurrencyScope { get; }

    /// <summary>
    /// Checks whether the captured facts include an event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <returns>Whether an event of the type was read.</returns>
    public bool Has<TEvent>() => Events.Any(@event => @event.Content is TEvent);

    /// <summary>
    /// Selects facts of an event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <returns>The matching fact payloads.</returns>
    public IEnumerable<TEvent> EventsOf<TEvent>() => Events.Select(@event => @event.Content).OfType<TEvent>();

    /// <summary>
    /// Returns facts routed and guarded at this decision's tail.
    /// </summary>
    /// <param name="events">The facts. An empty collection represents an identical retry and carries no scope.</param>
    /// <returns>The routed facts with one scope labelled by this event source id.</returns>
    public EventsWithConcurrencyScopes Append(params object[] events) => Append((IEnumerable<object>)events);

    /// <summary>
    /// Returns facts routed and guarded at this decision's tail.
    /// </summary>
    /// <param name="events">The facts. An empty collection represents an identical retry and carries no scope.</param>
    /// <returns>The routed facts with one scope labelled by this event source id.</returns>
    public EventsWithConcurrencyScopes Append(IEnumerable<object> events)
    {
        var routed = Route.Route(EventSourceId, events).ToArray();

        return new(routed, routed.Length == 0 ? [] : [new(EventSourceId, ConcurrencyScope)]);
    }
}
