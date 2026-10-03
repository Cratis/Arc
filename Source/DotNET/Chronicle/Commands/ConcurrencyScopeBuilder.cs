// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Builder for creating concurrency scopes based on command context and metadata attributes.
/// </summary>
public static class ConcurrencyScopeBuilder
{
    /// <summary>
    /// Builds a concurrency scope for an append a command makes to a specific event source.
    /// </summary>
    /// <param name="commandContext">The command context containing the command.</param>
    /// <param name="strategy">The <see cref="IConcurrencyScopeStrategy"/> that resolves the expected sequence number.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> the append targets.</param>
    /// <returns>
    /// A concurrency scope when any metadata attribute has concurrency enabled, otherwise null so the event
    /// sequence keeps applying its configured strategy.
    /// </returns>
    /// <remarks>
    /// The scope is built per target event source, and the expected sequence number comes from the same
    /// strategy an unscoped append uses. Both matter. A scope with no expected sequence number is not
    /// <see cref="ConcurrencyScope.NotSet"/>, so it displaces the strategy the event sequence would otherwise
    /// apply, and the kernel then skips validation precisely because there is no sequence number to validate
    /// against — a command that declares concurrency would end up with strictly less protection than one that
    /// says nothing. And a single scope shared across every event source a command writes to would apply one
    /// stream's expected tail to all the others, which is wrong for each of them.
    /// </remarks>
    public static async Task<ConcurrencyScope?> BuildFor(
        CommandContext commandContext,
        IConcurrencyScopeStrategy strategy,
        EventSourceId eventSourceId)
    {
        var commandType = commandContext.Command.GetType();

        var eventStreamIdAttribute = commandType.GetCustomAttributes(typeof(EventStreamIdAttribute), false).FirstOrDefault() as EventStreamIdAttribute;
        var eventStreamTypeAttribute = commandType.GetCustomAttributes(typeof(EventStreamTypeAttribute), false).FirstOrDefault() as EventStreamTypeAttribute;
        var eventSourceTypeAttribute = commandType.GetCustomAttributes(typeof(EventSourceTypeAttribute), false).FirstOrDefault() as EventSourceTypeAttribute;

        var scopeByEventStreamId = eventStreamIdAttribute?.Concurrency ?? false;
        var scopeByEventStreamType = eventStreamTypeAttribute?.Concurrency ?? false;
        var scopeByEventSourceType = eventSourceTypeAttribute?.Concurrency ?? false;

        if (!scopeByEventStreamId && !scopeByEventStreamType && !scopeByEventSourceType)
        {
            var dimensions = commandContext.Values.TryGetValue(WellKnownCommandContextKeys.ConcurrencyDimensions, out var value) && value is ConcurrencyDimensions declared
                ? declared
                : ConcurrencyDimensions.None;
            if (dimensions == ConcurrencyDimensions.None)
            {
                return null;
            }

            return await strategy.GetScope(
                eventSourceId,
                eventStreamType: dimensions.HasFlag(ConcurrencyDimensions.EventStreamType) ? commandContext.GetEventStreamType() : null,
                eventStreamId: dimensions.HasFlag(ConcurrencyDimensions.EventStreamId) ? commandContext.GetEventStreamId() : null,
                eventSourceType: dimensions.HasFlag(ConcurrencyDimensions.EventSourceType) ? commandContext.GetEventSourceType() : null);
        }

        return await strategy.GetScope(
            eventSourceId,
            eventStreamType: scopeByEventStreamType ? commandContext.GetEventStreamType() : null,
            eventStreamId: scopeByEventStreamId ? commandContext.GetEventStreamId() : null,
            eventSourceType: scopeByEventSourceType ? commandContext.GetEventSourceType() : null);
    }

    /// <summary>
    /// Builds the guard for one event, from the routing that event is actually written with.
    /// </summary>
    /// <param name="commandContext">The command context containing the command.</param>
    /// <param name="strategy">The <see cref="IConcurrencyScopeStrategy"/> that resolves the expected sequence number.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> the append targets.</param>
    /// <param name="routing">The <see cref="EventRouting"/> the event is written with.</param>
    /// <returns>The <see cref="DerivedConcurrencyScope"/> for the event.</returns>
    /// <remarks>
    /// A scope built from attributes that declare concurrency is the caller's explicit choice and is built as before.
    /// A guard that only comes from the event source definition is implicit: it is a default, not a choice, so it is
    /// never allowed to stand in for the guard another event of the same event source id needs. When the event is
    /// written through a definition, nothing is built here at all and the event sequence derives the guard from that
    /// event's own definition and stream, which also rejects incompatible guards for one event source id. When a
    /// returned event overrides the definition with legacy routing, the guard is built from that event's routing, not
    /// the command's.
    /// </remarks>
    internal static async Task<DerivedConcurrencyScope> BuildFor(
        CommandContext commandContext,
        IConcurrencyScopeStrategy strategy,
        EventSourceId eventSourceId,
        EventRouting routing)
    {
        var commandType = commandContext.Command.GetType();
        var declaresConcurrency = commandType.GetCustomAttributes(false).Any(_ => _ is EventStreamIdAttribute { Concurrency: true } or EventStreamTypeAttribute { Concurrency: true } or EventSourceTypeAttribute { Concurrency: true });
        if (declaresConcurrency)
        {
            return new(await BuildFor(commandContext, strategy, eventSourceId), DerivedConcurrencyScopeOrigin.Explicit);
        }

        var dimensions = commandContext.Values.TryGetValue(WellKnownCommandContextKeys.ConcurrencyDimensions, out var value) && value is ConcurrencyDimensions declared
            ? declared
            : ConcurrencyDimensions.None;
        if (dimensions == ConcurrencyDimensions.None)
        {
            return new(null, DerivedConcurrencyScopeOrigin.None);
        }

        if (routing.EventSource is not null)
        {
            return new(null, DerivedConcurrencyScopeOrigin.DerivedByEventSequence);
        }

        var scope = await strategy.GetScope(
            eventSourceId,
            eventStreamType: dimensions.HasFlag(ConcurrencyDimensions.EventStreamType) ? routing.EventStreamType : null,
            eventStreamId: dimensions.HasFlag(ConcurrencyDimensions.EventStreamId) ? routing.EventStreamId : null,
            eventSourceType: dimensions.HasFlag(ConcurrencyDimensions.EventSourceType) ? routing.EventSourceType : null);
        return new(scope, DerivedConcurrencyScopeOrigin.Implicit);
    }
}
