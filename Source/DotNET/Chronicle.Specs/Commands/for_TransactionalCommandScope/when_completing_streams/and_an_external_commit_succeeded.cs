// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class and_an_external_commit_succeeded : given.a_real_pending_completion
{
    void Establish()
    {
        _realUnitOfWork.AddEvents(EventSequenceId.Log, [new(EventSourceId.New(), new object())], []);
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(AppendManyResult.Success(_correlationId, [EventSequenceNumber.First]));
    }

    async Task Because()
    {
        _result.MergeWith(await Enroll());
        await _realUnitOfWork.Commit();
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_not_complete_the_stream() => _operations.ShouldBeEmpty();
    [Fact] void should_fail_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_unknown_outcome() => _result.ExceptionMessages.Single().ShouldContain("commit outcome is unknown");
}
