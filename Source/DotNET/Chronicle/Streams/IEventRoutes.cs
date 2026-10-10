// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Streams;

/// <summary>
/// Resolves routes from the event store's discovered event source definitions.
/// </summary>
public interface IEventRoutes
{
    /// <summary>
    /// Resolves the route for an event source definition.
    /// </summary>
    /// <typeparam name="TSource">The event source definition type.</typeparam>
    /// <param name="stream">The selected stream, if any.</param>
    /// <returns>The route with the default stream id.</returns>
    EventRoute For<TSource>(string? stream = default)
        where TSource : IEventSource;

    /// <summary>
    /// Resolves the route for an event source definition.
    /// </summary>
    /// <param name="eventSource">The event source definition type.</param>
    /// <param name="stream">The selected stream, if any.</param>
    /// <returns>The route with the default stream id.</returns>
    EventRoute For(Type eventSource, string? stream = default);
}
