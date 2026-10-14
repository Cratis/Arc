// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentContributor.when_enrolling;

public class with_a_rejected_multi_source_batch : given.a_contributor
{
    Exception _error;
    void Establish() => _receivingUnit.AddEnrollmentContributor(_contributor);
    void Because() => _error = Catch.Exception(() => _receivingUnit.AddEvents(EventSequenceId.Log,
        [new(EventSourceId.New(), new Created("unrelated")), new(_id, new Created("guarded"))],
        [new KeyValuePair<EventSourceId, ConcurrencyScope>(_id, ConcurrencyScope.None)]));

    [Fact] void should_reject_before_staging() => _error.ShouldBeOfExactType<ConflictingConcurrencyScopes>();
    [Fact] void should_stage_no_events() => _receivingUnit.GetEvents().ShouldBeEmpty();
    [Fact] void should_not_record_provenance() => FreshIdentifierScopes.For(_receivingUnit).ShouldBeNull();
}
