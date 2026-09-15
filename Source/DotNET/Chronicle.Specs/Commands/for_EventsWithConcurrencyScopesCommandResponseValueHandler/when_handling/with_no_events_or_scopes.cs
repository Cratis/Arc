// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class with_no_events_or_scopes : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    CommandResult _result;

    void Establish() => _eventLog.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
        .Returns(_ => Task.FromException<AppendManyResult>(new ArgumentException("At least one event is required.")));

    async Task Because() => _result = await _handler.Handle(_commandContext, new EventsWithConcurrencyScopes([], []));

    [Fact] void should_succeed_without_appending() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_command_correlation() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_not_enqueue_a_transaction_batch() => _unitOfWork.AddEventsCallCount.ShouldEqual(0);
    [Fact] void should_not_call_append_many() => _eventLog.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>());
}
