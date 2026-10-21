// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentContributor.when_enrolling;

public class with_a_guarded_capture : given.a_contributor
{
    void Because()
    {
        _scope = _contributor.GetRequiredScope(_enrollment);
        _contributor.Enrolled(_enrollment, _scope);
    }

    [Fact] void should_require_whole_source_emptiness() => _scope.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_target_the_generated_source() => _scope.EventSourceId.ShouldEqual(_id);
    [Fact] void should_not_narrow_the_guard() => (_scope.EventStreamType is null && _scope.EventStreamId is null && _scope.EventSourceType is null && _scope.EventTypes is null).ShouldBeTrue();
    [Fact] void should_record_successful_provenance() => FreshIdentifierScopes.For(_receivingUnit)!.Contains(EventSequenceId.Log, _id).ShouldBeTrue();
    [Fact] void should_not_mutate_the_unit_from_callbacks() => _receivingUnit.GetEvents().ShouldBeEmpty();
}
