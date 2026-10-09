// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Streams;

/// <summary>
/// Represents definition-backed route resolution.
/// </summary>
/// <param name="eventSources">The event store's discovered definitions.</param>
public class EventRoutes(IEventSources eventSources) : IEventRoutes
{
    /// <inheritdoc/>
    public EventRoute For<TSource>(string? stream = default)
        where TSource : IEventSource => For(typeof(TSource), stream);

    /// <inheritdoc/>
    public EventRoute For(Type eventSource, string? stream = default) => EventRoute.For(eventSources.GetFor(eventSource), stream);
}
