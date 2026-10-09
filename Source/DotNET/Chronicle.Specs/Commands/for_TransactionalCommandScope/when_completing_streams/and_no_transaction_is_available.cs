// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class and_no_transaction_is_available : given.a_pending_completion
{
    async Task Because()
    {
        _context = _context with { ServiceProvider = null };
        _result.MergeWith(await Enroll());
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>()).Returns(_ =>
        {
            _operations.Add("immediate append");
            return AppendManyResult.Success(_correlationId, [EventSequenceNumber.First]);
        });
        var append = new EventsWithConcurrencyScopesCommandResponseValueHandler(_eventLog);
        var events = new EventsWithConcurrencyScopes([new(EventSourceId.New(), new object())], []);
        _result.MergeWith(await append.Handle(_context, events));
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_complete_after_immediate_appends() => _operations.ShouldEqual(["immediate append", "complete"]);
    [Fact] void should_not_commit_a_transaction() => _unitOfWork.DidNotReceive().Commit();
}
