// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Provides definition-based event routing values for commands declaring <see cref="EventSourceAttribute{TSource}"/>.
/// </summary>
/// <param name="eventSources">The discovered event source definitions.</param>
public class EventSourceDefinitionValuesProvider(IEventSources eventSources) : ICommandContextValuesProvider
{
    /// <inheritdoc/>
    public CommandContextValues Provide(object command)
    {
        var commandType = command.GetType();
        var declaration = commandType.GetCustomAttributes(false).OfType<IEventSourceDeclaration>().SingleOrDefault();
        if (declaration is null)
        {
            return [];
        }

        var definition = eventSources.GetFor(declaration.EventSource);
        var stream = declaration.Stream is null ? null : definition.FindStream(declaration.Stream)
            ?? throw new EventRoutingContradictsEventSource(commandType, nameof(EventStreamType), "a stream declared by the event source", declaration.Stream);
        VerifyStringRouting(commandType, definition, stream);

        return new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSource, declaration.EventSource },
            { WellKnownCommandContextKeys.EventStream, declaration.Stream! },
            { WellKnownCommandContextKeys.ConcurrencyDimensions, definition.ConcurrencyFor(stream) },
            { WellKnownCommandContextKeys.EventSourceType, definition.EventSourceType },
            { WellKnownCommandContextKeys.EventStreamType, stream?.EventStreamType ?? EventStreamType.All }
        };
    }

    static void VerifyStringRouting(Type commandType, EventSourceDefinition definition, EventStream? stream)
    {
        var sourceType = commandType.GetCustomAttributes(typeof(EventSourceTypeAttribute), false).OfType<EventSourceTypeAttribute>().SingleOrDefault();
        if (sourceType is not null && sourceType.EventSourceType != definition.EventSourceType)
        {
            throw new EventRoutingContradictsEventSource(commandType, nameof(EventSourceType), definition.Name, sourceType.EventSourceType);
        }

        var streamType = commandType.GetCustomAttributes(typeof(EventStreamTypeAttribute), false).OfType<EventStreamTypeAttribute>().SingleOrDefault();
        if (streamType is not null && streamType.EventStreamType != (stream?.EventStreamType ?? EventStreamType.All))
        {
            throw new EventRoutingContradictsEventSource(commandType, nameof(EventStreamType), stream?.Name ?? EventStreamType.All, streamType.EventStreamType);
        }
    }
}
