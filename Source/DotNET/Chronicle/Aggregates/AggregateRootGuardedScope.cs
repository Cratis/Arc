// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Aggregates;

/// <summary>
/// The event source type, event stream type and event stream id an aggregate root's commit guards, and that
/// rehydration captures its tail for.
/// </summary>
/// <param name="EventSourceType">The guarded <see cref="EventSourceType"/>; <see cref="EventSourceType.Default"/> guards any.</param>
/// <param name="EventStreamType">The guarded <see cref="EventStreamType"/>; <see cref="EventStreamType.All"/> guards any.</param>
/// <param name="EventStreamId">The guarded <see cref="EventStreamId"/>; the default stream id guards any.</param>
internal record AggregateRootGuardedScope(EventSourceType EventSourceType, EventStreamType EventStreamType, EventStreamId EventStreamId)
{
    /// <summary>
    /// Resolve the scope guarded for an aggregate root context.
    /// </summary>
    /// <param name="context">The <see cref="IAggregateRootContext"/> to resolve for.</param>
    /// <returns>The <see cref="AggregateRootGuardedScope"/>.</returns>
    /// <remarks>
    /// An aggregate root that declares its event source rehydrates only from its own stream, so the scope is that
    /// stream. One that does not rehydrates from every handled event for its event source id, whatever stream it was
    /// appended to, so the scope uses the values Chronicle treats as "any" (#2796). Its mutation narrows the guard
    /// to handled event types when the mutator exposes a nonempty set, while rehydration captures the unfiltered
    /// tail as the expected sequence number. The events the aggregate appends keep their own routing either way.
    /// </remarks>
    public static AggregateRootGuardedScope For(IAggregateRootContext context) =>
        context.HasDeclaredEventSource()
            ? new(context.EventSourceType, context.EventStreamType, context.EventStreamId)
            : new(EventSourceType.Default, EventStreamType.All, EventStreamId.Default);

    /// <summary>
    /// Check whether this scope is exactly the aggregate root's own stream.
    /// </summary>
    /// <param name="context">The <see cref="IAggregateRootContext"/> to check against.</param>
    /// <returns>True if the scope is the context's own stream, false if not.</returns>
    public bool IsOwnStreamOf(IAggregateRootContext context) =>
        EventSourceType == context.EventSourceType &&
        EventStreamType == context.EventStreamType &&
        EventStreamId == context.EventStreamId;
}
