// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandContextValuesProvider"/> that provides values for the event stream id.
/// </summary>
public class EventStreamIdValuesProvider : ICommandContextValuesProvider
{
    /// <inheritdoc/>
    public CommandContextValues Provide(object command)
    {
        var attribute = command.GetType().GetCustomAttribute<EventStreamIdAttribute>(false);
        if (command is not ICanProvideEventStreamId && attribute is not null && EventStreamIdTemplate.IsTemplate(attribute.Value.Value))
        {
            // Template parts may be missing until validation succeeds. Resolve them when the route is needed.
            return [];
        }
        var id = EventStreamIdTemplate.ResolveFor(command);

        return id is null ? [] : new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventStreamId, id }
        };
    }
}
