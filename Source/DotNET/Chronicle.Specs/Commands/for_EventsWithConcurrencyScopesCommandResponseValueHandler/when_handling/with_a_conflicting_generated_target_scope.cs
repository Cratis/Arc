// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class with_a_conflicting_generated_target_scope : Commands.given.a_generated_target
{
    Exception? _error;

    void Establish() => CommandTransaction.Current = _unit;

    async Task Because() => _error = await Catch.Exception(() => new EventsWithConcurrencyScopesCommandResponseValueHandler(_eventLog, _eventSources).Handle(_context, new EventsWithConcurrencyScopes([new(_id, new Created("new"))], [new KeyValuePair<EventSourceId, ConcurrencyScope>(_id, new(EventSequenceNumber.BeforeFirst, EventSourceId: _id, EventStreamType: new("narrowed")))])));

    [Fact] void should_report_conflicting_intents() => _error.ShouldBeOfExactType<ConflictingConcurrencyScopes>();
    [Fact] void should_enroll_nothing() => _enrolled.ShouldBeNull();
}
