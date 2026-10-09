// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class and_the_immediate_append_failed : given.a_pending_completion
{
    async Task Because()
    {
        _context = _context with { ServiceProvider = null };
        _result.MergeWith(await Enroll());
        var id = EventSourceId.New();
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>()).Returns(new AppendManyResult
        {
            CorrelationId = _correlationId,
            ConcurrencyViolations = [new(id, EventSequenceNumber.First, new(1))]
        });
        var append = new EventsWithConcurrencyScopesCommandResponseValueHandler(_eventLog);
        _result.MergeWith(await append.Handle(_context, new EventsWithConcurrencyScopes([new(id, new object())], [])));
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_not_complete_the_stream() => _operations.ShouldBeEmpty();
}
