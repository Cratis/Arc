// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandContextValuesProvider"/> that provides values for the event source id.
/// </summary>
/// <param name="logger">The <see cref="ILogger"/> to use for logging.</param>
public class EventSourceValuesProvider(ILogger<EventSourceValuesProvider> logger) : ICommandContextValuesProvider
{
    /// <inheritdoc/>
    public CommandContextValues Provide(object command)
    {
        var eventSourceId = ResolveEventSourceId(command);

        // Also expose the id as the provider-neutral resolved key so a read model backing provider that does not depend
        // on Chronicle (for example Entity Framework Core) can load a read model by the same key. An unspecified id
        // carries no usable key, so the neutral key is empty in that case.
        return new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, eventSourceId },
            { Cratis.Arc.Commands.CommandContextKeys.ResolvedKey, NeutralKeyFrom(eventSourceId) }
        };
    }

    /// <summary>
    /// Converts an event source id into the provider-neutral resolved key string.
    /// </summary>
    /// <param name="eventSourceId">The event source id to convert.</param>
    /// <returns>The id value as a string, or an empty string when the id is unspecified.</returns>
    static string NeutralKeyFrom(EventSourceId eventSourceId) =>
        eventSourceId == EventSourceId.Unspecified ? string.Empty : eventSourceId.Value;

    /// <summary>
    /// Resolves the event source id for a command, from a self-composing command or from a key property.
    /// </summary>
    /// <param name="command">The command to resolve the event source id for.</param>
    /// <returns>The resolved event source id, or <see cref="EventSourceId.Unspecified"/> when none could be composed.</returns>
    EventSourceId ResolveEventSourceId(object command)
    {
        if (command is ICanProvideEventSourceId provider)
        {
            return ProvidedEventSourceIdOrUnspecified(provider);
        }

        var eventSourceId = EventSourceId.New();
        if (command.HasEventSourceId())
        {
            eventSourceId = command.GetEventSourceId();
        }

        return eventSourceId;
    }

    /// <summary>
    /// Asks the command for its event source id, falling back to <see cref="EventSourceId.Unspecified"/> when it cannot be composed.
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
    /// <see cref="EventSourceExtensions.GetEventSourceId(object)"/>. It is logged at debug level because the usual cause is
    /// bad input rather than a defect, and logging louder would let a hostile caller flood the log — but it is logged,
    /// because the same path is where a genuine defect inside an implementation would otherwise vanish silently.
    /// </remarks>
    EventSourceId ProvidedEventSourceIdOrUnspecified(ICanProvideEventSourceId provider)
    {
        try
        {
            var eventSourceId = provider.GetEventSourceId();
            if (eventSourceId is not null)
            {
                return eventSourceId;
            }

            logger.CouldNotComposeProvidedEventSourceId(provider.GetType().Name, null);
        }
        catch (Exception ex)
        {
            logger.CouldNotComposeProvidedEventSourceId(provider.GetType().Name, ex);
        }

        return EventSourceId.Unspecified;
    }
}
