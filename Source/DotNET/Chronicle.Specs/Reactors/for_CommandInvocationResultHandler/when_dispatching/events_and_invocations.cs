// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandInvocationResultHandler.when_dispatching;

public class events_and_invocations : given.an_invocation_dispatcher
{
    List<string> _order = [];

    void Establish()
    {
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>()).Returns(call =>
        {
            var events = call.Arg<IEnumerable<EventForEventSourceId>>().Select(envelope => envelope.Event).ToArray();
            _batches.Add(events);
            _order.Add("append");
            return AppendManyResult.Success(CorrelationId.NotSet, [EventSequenceNumber.First]);
        });
        _commandPipeline.Execute(_command, _serviceProvider).Returns(_ =>
        {
            _order.Add("command");
            return _commandResult;
        });
    }
    async Task Because() => _result = await _dispatcher.Handle(_context, _store, new object[] { _invocation, new RefusalRecorded("ordinary"), _later });

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_events_first_then_branch_append_order() => _order.SequenceEqual(["append", "command", "append"]).ShouldBeTrue();
    [Fact] void should_append_ordinary_events_first() => _batches.First().Single().ShouldEqual(new RefusalRecorded("ordinary"));
    [Fact] void should_append_branch_events_separately() => _batches.Last().Single().ShouldEqual(new RefusalRecorded("$strings.refused"));
    [Fact] void should_skip_later_commands() => _commandPipeline.DidNotReceive().Execute(_later, _serviceProvider);
}
