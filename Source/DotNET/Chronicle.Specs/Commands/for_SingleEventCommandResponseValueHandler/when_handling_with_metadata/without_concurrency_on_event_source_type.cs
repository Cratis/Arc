// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventCommandResponseValueHandler.when_handling_with_metadata;

/// <summary>
/// A routing-only metadata attribute - declared without <c>concurrency: true</c> - builds no concurrency scope, and
/// that is where every reading of this used to stop. What it also does is reach the append, because none of the
/// handlers condition the metadata on the flag.
/// </summary>
/// <remarks>
/// Both halves are asserted together deliberately, because the pair is the whole point and either alone reads as
/// the opposite conclusion. A null scope is not the end of the story: the event sequence substitutes one when the
/// caller supplies none, resolving it from the very metadata passed alongside - so the tag ends up narrowing the
/// check that the flag was believed to govern. Asserting the null scope on its own is what let
/// <c>and_the_command_carries_metadata_without_concurrency</c> read as "metadata alone leaves the append exactly as
/// it was", which is true of the scope Arc builds and false of the check the append gets.
/// </remarks>
public class without_concurrency_on_event_source_type : given.a_single_event_command_response_value_handler
{
    const string RoutingOnlyEventSourceType = "Customer";

    TestEvent _event;
    CommandResult _result;

    void Establish()
    {
        _event = new TestEvent("Single Event");

        var command = new TestCommand { EventSourceId = EventSourceId.New() };
        var commandContextValues = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceId, command.EventSourceId },
            { WellKnownCommandContextKeys.EventSourceType, new EventSourceType(RoutingOnlyEventSourceType) }
        };
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), command, [], commandContextValues, null);
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _event);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();

    [Fact] void should_append_without_a_concurrency_scope() => _eventLog.Received().Append(
        Arg.Any<EventSourceId>(),
        Arg.Any<object>(),
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Any<EventSourceType?>(),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        null);

    [Fact] void should_still_carry_the_routing_metadata_to_the_append() => _eventLog.Received().Append(
        Arg.Any<EventSourceId>(),
        Arg.Any<object>(),
        Arg.Any<EventStreamType?>(),
        Arg.Any<EventStreamId?>(),
        Arg.Is<EventSourceType?>(_ => _ != null && _.Value == RoutingOnlyEventSourceType),
        Arg.Any<CorrelationId?>(),
        Arg.Any<IEnumerable<string>?>(),
        Arg.Any<ConcurrencyScope?>());

    [EventSourceType(RoutingOnlyEventSourceType)]
    class TestCommand
    {
        public EventSourceId EventSourceId { get; set; } = EventSourceId.Unspecified;
    }
}
