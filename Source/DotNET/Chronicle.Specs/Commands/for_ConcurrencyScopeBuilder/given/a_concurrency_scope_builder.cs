// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_ConcurrencyScopeBuilder.given;

public class a_concurrency_scope_builder : Specification
{
    protected const string EventSourceTypeValue = "Thing";

    protected IConcurrencyScopeStrategy _strategy;
    protected EventSourceId _eventSourceId;
    protected ConcurrencyScope _resolvedScope;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _strategy = Substitute.For<IConcurrencyScopeStrategy>();

        // The tail an optimistic strategy would resolve for the stream. Seeded with an actual value, because a
        // scope whose sequence number is not an actual value is exactly what the kernel skips validating.
        _resolvedScope = new ConcurrencyScope(42UL, _eventSourceId, EventSourceType: EventSourceTypeValue);
        _strategy.GetScope(
                Arg.Any<EventSourceId>(),
                Arg.Any<EventStreamType?>(),
                Arg.Any<EventStreamId?>(),
                Arg.Any<EventSourceType?>(),
                Arg.Any<IEnumerable<EventType>?>())
            .Returns(_resolvedScope);
    }

    protected static CommandContext CommandContextFor(object command) =>
        new(CorrelationId.New(), command.GetType(), command, [], new CommandContextValues(), null);

    [EventSourceType(EventSourceTypeValue, concurrency: true)]
    public class CommandScopedForConcurrency;

    [EventSourceType(EventSourceTypeValue)]
    public class CommandCarryingMetadataWithoutConcurrency;

    public class CommandWithoutMetadata;
}
