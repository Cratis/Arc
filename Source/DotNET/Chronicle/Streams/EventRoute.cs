// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

using EventRoutingContradictsEventSource = Cratis.Arc.Chronicle.Commands.EventRoutingContradictsEventSource;

namespace Cratis.Arc.Chronicle.Streams;

/// <summary>
/// Represents the routing shared by a command, its reads and its returned events.
/// </summary>
/// <param name="EventSourceType">The event source type.</param>
/// <param name="EventStreamType">The event stream type.</param>
/// <param name="EventStreamId">The event stream id.</param>
public sealed record EventRoute(EventSourceType EventSourceType, EventStreamType EventStreamType, EventStreamId EventStreamId)
{
    /// <summary>
    /// Gets the event source definition type, when routing through a definition.
    /// </summary>
    public Type? EventSource { get; init; }

    /// <summary>
    /// Gets the selected stream name from the definition.
    /// </summary>
    public string? EventStream { get; init; }

    /// <summary>
    /// Gets the definition's concurrency policy.
    /// </summary>
    public ConcurrencyDimensions Concurrency { get; init; }

    /// <summary>
    /// Resolves routing from an already discovered definition.
    /// </summary>
    /// <param name="definition">The event source definition.</param>
    /// <param name="stream">The selected stream, if any.</param>
    /// <returns>The resolved route, with the default stream id.</returns>
    /// <exception cref="EventRoutingContradictsEventSource">The stream is not declared by the definition.</exception>
    public static EventRoute For(EventSourceDefinition definition, string? stream = default) => For(definition, stream, definition.ClrType);

    /// <summary>
    /// Selects the stream id while retaining the definition and its policy.
    /// </summary>
    /// <param name="streamId">The stream id.</param>
    /// <returns>The route with the selected stream id.</returns>
    public EventRoute WithStreamId(EventStreamId streamId) => this with { EventStreamId = streamId };

    /// <summary>
    /// Routes an event to this route.
    /// </summary>
    /// <param name="id">The event source id.</param>
    /// <param name="event">The fact to append.</param>
    /// <returns>The routed event wrapper.</returns>
    public EventForEventSourceId Route(EventSourceId id, object @event) => new(id, @event)
    {
        EventSourceType = EventSourceType,
        EventStreamType = EventStreamType,
        EventStreamId = EventStreamId,
        EventSource = EventSource,
        EventStream = EventStream
    };

    /// <summary>
    /// Routes every event to this route.
    /// </summary>
    /// <param name="id">The event source id.</param>
    /// <param name="events">The facts to append.</param>
    /// <returns>The routed event wrappers.</returns>
    public IEnumerable<EventForEventSourceId> Route(EventSourceId id, IEnumerable<object> events) => events.Select(@event => Route(id, @event));

    /// <summary>
    /// Resolves a definition route while naming the declaring command in routing failures.
    /// </summary>
    /// <param name="definition">The discovered definition.</param>
    /// <param name="stream">The stream name.</param>
    /// <param name="declaringType">The declaration carrying the route.</param>
    /// <returns>The resolved route.</returns>
    /// <exception cref="EventRoutingContradictsEventSource">The stream is not declared by the definition.</exception>
    internal static EventRoute For(EventSourceDefinition definition, string? stream, Type declaringType)
    {
        var selected = stream is null ? null : definition.FindStream(stream)
            ?? throw new EventRoutingContradictsEventSource(declaringType, nameof(EventStreamType), "a stream declared by the event source", stream);

        return new(definition.EventSourceType, selected?.EventStreamType ?? EventStreamType.All, Cratis.Chronicle.Events.EventStreamId.Default)
        {
            EventSource = definition.ClrType,
            EventStream = stream,
            Concurrency = definition.ConcurrencyFor(selected)
        };
    }
}
