// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentContributor.when_enrolling;

public class with_a_scope_only_batch_entry : given.a_contributor
{
    void Establish() => _receivingUnit.AddEnrollmentContributor(_contributor);
    void Because() => _receivingUnit.AddEvents(EventSequenceId.Log, [], [new KeyValuePair<Cratis.Chronicle.Events.EventSourceId, ConcurrencyScope>(_id, ConcurrencyScope.None)]);

    [Fact] void should_not_create_a_freshness_guard_without_events() => _freshness.HasGuard(EventSequenceId.Log, _id).ShouldBeFalse();
    [Fact] void should_not_record_generated_provenance() => FreshIdentifierScopes.For(_receivingUnit).ShouldBeNull();
    [Fact] void should_not_stage_events() => _receivingUnit.GetEvents().ShouldBeEmpty();
}
