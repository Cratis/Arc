// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandContextValuesProvider"/> that provides values for the event stream id.
/// </summary>
public class EventStreamIdValuesProvider : ICommandContextValuesProvider
{
    /// <inheritdoc/>
    public CommandContextValues Provide(object command)
    {
        var id = EventStreamIdTemplate.ResolveFor(command);

        return id is null ? [] : new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventStreamId, id }
        };
    }
}
