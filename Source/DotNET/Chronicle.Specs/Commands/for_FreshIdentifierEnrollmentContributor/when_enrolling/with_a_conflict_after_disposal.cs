// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentContributor.when_enrolling;

public class with_a_conflict_after_disposal : given.a_contributor
{
    Exception _error;
    void Establish()
    {
        using var registration = _receivingUnit.AddEnrollmentContributor(_contributor);
        _receivingUnit.AddEvents(EventSequenceId.Log, [new(_id, new Created("first"))], []);
    }
    void Because() => _error = Catch.Exception(() => _receivingUnit.AddEvents(EventSequenceId.Log, [new(_id, new Created("conflict"))], [new KeyValuePair<Cratis.Chronicle.Events.EventSourceId, ConcurrencyScope>(_id, ConcurrencyScope.None)]));

    [Fact] void should_keep_enforcing_the_required_scope() => _error.ShouldBeOfExactType<ConflictingEnrollmentScope>();
    [Fact] void should_not_stage_the_conflicting_event() => _receivingUnit.GetEvents().Count().ShouldEqual(1);
}
