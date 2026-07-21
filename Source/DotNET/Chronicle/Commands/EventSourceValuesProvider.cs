// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandContextValuesProvider"/> that provides values for the event source id.
/// </summary>
public class EventSourceValuesProvider : ICommandContextValuesProvider
{
    /// <inheritdoc/>
    public CommandContextValues Provide(object command)
    {
        if (command is ICanProvideEventSourceId provider)
        {
            return new CommandContextValues
            {
                { WellKnownCommandContextKeys.EventSourceId, ProvidedEventSourceIdOrUnspecified(provider) }
            };
        }

        var eventSourceId = EventSourceId.New();
        if (command.HasEventSourceId())
        {
            eventSourceId = command.GetEventSourceId();
        }

        return new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, eventSourceId }
        };
    }

    /// <summary>
    /// Asks the command for its event source id, falling back to <see cref="EventSourceId.Unspecified"/> when it throws.
    /// </summary>
    /// <param name="provider">The command providing its own event source id.</param>
    /// <returns>The provided event source id, or <see cref="EventSourceId.Unspecified"/> when it could not be composed.</returns>
    /// <remarks>
    /// A command composes its key from its own properties, so hostile or partial input leaves it composing a null concept —
    /// most often an implicit conversion on a generic <see cref="EventSourceId{T}"/> that throws a
    /// <see cref="NullReferenceException"/>. This runs while the command context is being built, before any
    /// <see cref="ICommandFilter"/>, so a throw here escapes the filter chain's per-filter handling entirely and surfaces
    /// as an unhandled server error (HTTP 500) rather than the 400 the input deserves. Treating an uncomposable key as an
    /// unspecified id lets the command reach the filter and validation stages that turn it into a clean response — the same
    /// contract the reflection-based fallback already honors through
    /// <see cref="EventSourceExtensions.GetEventSourceId(object)"/>.
    /// </remarks>
    static EventSourceId ProvidedEventSourceIdOrUnspecified(ICanProvideEventSourceId provider)
    {
        try
        {
            return provider.GetEventSourceId() ?? EventSourceId.Unspecified;
        }
        catch (Exception)
        {
            return EventSourceId.Unspecified;
        }
    }
}
