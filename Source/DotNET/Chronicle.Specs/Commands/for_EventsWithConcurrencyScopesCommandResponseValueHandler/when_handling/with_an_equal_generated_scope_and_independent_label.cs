// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class with_an_equal_generated_scope_and_independent_label : Commands.given.a_generated_target
{
    readonly EventSourceId _label = new("independent-label");
    readonly ConcurrencyScope _independent = new(EventSequenceNumber.BeforeFirst, EventStreamType: new("independent-stream"));

    async Task Because()
    {
        CommandTransaction.Current = _unit;
        _result = await new EventsWithConcurrencyScopesCommandResponseValueHandler(_eventLog, _eventSources).Handle(
            _context,
            new EventsWithConcurrencyScopes([new(_id, new Created("new"))], [new KeyValuePair<EventSourceId, ConcurrencyScope>(_id, new(EventSequenceNumber.BeforeFirst, EventSourceId: _id)), new(_label, _independent)]));
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_independent_label() => _enrolledScopes.Single(scope => scope.Key == _label).Value.ShouldBeSame(_independent);
    [Fact] void should_enroll_the_generated_guard_once() => _enrolledScopes.Count(scope => scope.Key == _id).ShouldEqual(1);
    [Fact] void should_record_generated_provenance() => FreshIdentifierScopes.For(_unit)!.Owns(new(_id, EventSequenceNumber.BeforeFirst, EventSequenceNumber.First)).ShouldBeTrue();
}
