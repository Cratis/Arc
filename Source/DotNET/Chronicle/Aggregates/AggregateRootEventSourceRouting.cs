// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// The routing an aggregate root resolves from its <see cref="EventSourceAttribute{TSource}"/> declaration.
/// </summary>
/// <param name="EventSource">The type of the event source definition.</param>
/// <param name="EventStream">The declared stream name, if any.</param>
/// <param name="EventSourceType">The resolved <see cref="EventSourceType"/>.</param>
/// <param name="EventStreamType">The resolved <see cref="EventStreamType"/>.</param>
public record AggregateRootEventSourceRouting(Type EventSource, string? EventStream, EventSourceType EventSourceType, EventStreamType EventStreamType)
{
    /// <summary>
    /// Resolve the routing declared by an aggregate root type.
    /// </summary>
    /// <param name="aggregateRootType">The type of the aggregate root.</param>
    /// <param name="getEventSources">Gets the <see cref="IEventSources"/> holding the discovered definitions; only called when the aggregate root declares an event source.</param>
    /// <param name="requestedEventSourceType">An event source type explicitly requested for the aggregate root, if any.</param>
    /// <returns>The routing, or null when the aggregate root declares no event source.</returns>
    /// <exception cref="AggregateRootContradictsEventSource">Thrown when the declaration contradicts the stream, the requested source type, or an event stream type attribute.</exception>
    public static AggregateRootEventSourceRouting? Resolve(Type aggregateRootType, Func<IEventSources> getEventSources, EventSourceType? requestedEventSourceType = default)
    {
        var declaration = aggregateRootType.GetCustomAttributes(false).OfType<IEventSourceDeclaration>().SingleOrDefault();
        if (declaration is null)
        {
            return null;
        }

        var definition = getEventSources().GetFor(declaration.EventSource);
        var stream = declaration.Stream is null ? null : definition.FindStream(declaration.Stream)
            ?? throw new AggregateRootContradictsEventSource(aggregateRootType, nameof(EventStreamType), "a stream declared by the event source", declaration.Stream);

        if (requestedEventSourceType is not null && requestedEventSourceType != EventSourceType.Default && requestedEventSourceType != definition.EventSourceType)
        {
            throw new AggregateRootContradictsEventSource(aggregateRootType, nameof(EventSourceType), definition.Name, requestedEventSourceType);
        }

        var explicitStreamType = aggregateRootType.GetCustomAttributes(typeof(EventStreamTypeAttribute), false).OfType<EventStreamTypeAttribute>().SingleOrDefault();
        if (stream is not null && explicitStreamType is not null && explicitStreamType.EventStreamType != stream.EventStreamType)
        {
            throw new AggregateRootContradictsEventSource(aggregateRootType, nameof(EventStreamType), stream.Name, explicitStreamType.EventStreamType);
        }

        return new(declaration.EventSource, declaration.Stream, definition.EventSourceType, stream?.EventStreamType ?? explicitStreamType?.EventStreamType ?? aggregateRootType.Name);
    }
}
