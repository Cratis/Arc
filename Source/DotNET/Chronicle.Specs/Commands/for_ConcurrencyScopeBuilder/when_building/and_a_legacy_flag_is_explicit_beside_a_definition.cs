// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_ConcurrencyScopeBuilder.when_building;

/// <summary>
/// An explicit legacy concurrency flag wins over the dimensions of the definition.
/// </summary>
public class and_a_legacy_flag_is_explicit_beside_a_definition : given.a_concurrency_scope_builder
{
    ConcurrencyScope? _result;

    async Task Because() => _result = await ConcurrencyScopeBuilder.BuildFor(ContextFor(new LegacyStreamTypeWithConcurrency(), ConcurrencyDimensions.EventSourceType | ConcurrencyDimensions.EventStreamId), _strategy, _eventSourceId);

    [Fact] void should_build_a_scope() => _result.ShouldNotBeNull();
    [Fact] void should_scope_by_what_the_flag_selects_only() => _strategy.Received(1).GetScope(_eventSourceId, new EventStreamType("Transactions"), null, null, Arg.Any<IEnumerable<EventType>?>());

    static CommandContext ContextFor(object command, ConcurrencyDimensions dimensions)
    {
        var values = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceType, new EventSourceType("Account") },
            { WellKnownCommandContextKeys.EventStreamType, new EventStreamType("Transactions") },
            { WellKnownCommandContextKeys.EventStreamId, new EventStreamId("2026-10") },
            { WellKnownCommandContextKeys.ConcurrencyDimensions, dimensions }
        };

        return new(CorrelationId.New(), command.GetType(), command, [], values, null);
    }

    [EventSourceType("Legacy")]
    public class LegacyAttributeWithoutConcurrency;

    [EventStreamType("LegacyStream", concurrency: true)]
    public class LegacyStreamTypeWithConcurrency;

    public class CommandWithoutAttributes;
}
