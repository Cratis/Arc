// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Provides definition-based event routing values for commands declaring <see cref="EventSourceAttribute{TSource}"/>.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> the discovered event source definitions are resolved from, only when a command declares one.</param>
public class EventSourceDefinitionValuesProvider(IServiceProvider serviceProvider) : ICommandContextValuesProvider
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

        var definition = serviceProvider.GetRequiredService<IEventSources>().GetFor(declaration.EventSource);
        var route = EventRoute.For(definition, declaration.Stream, commandType);
        VerifyStringRouting(commandType, definition, declaration.Stream is null ? null : definition.FindStream(declaration.Stream));

        return new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSource, declaration.EventSource },
            { WellKnownCommandContextKeys.EventStream, declaration.Stream! },
            { WellKnownCommandContextKeys.ConcurrencyDimensions, route.Concurrency },
            { WellKnownCommandContextKeys.EventSourceType, route.EventSourceType },
            { WellKnownCommandContextKeys.EventStreamType, route.EventStreamType }
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
