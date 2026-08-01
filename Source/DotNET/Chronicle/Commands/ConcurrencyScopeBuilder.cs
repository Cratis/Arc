// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

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
            return null;
        }

        return await strategy.GetScope(
            eventSourceId,
            eventStreamType: scopeByEventStreamType ? commandContext.GetEventStreamType() : null,
            eventStreamId: scopeByEventStreamId ? commandContext.GetEventStreamId() : null,
            eventSourceType: scopeByEventSourceType ? commandContext.GetEventSourceType() : null);
    }
}
