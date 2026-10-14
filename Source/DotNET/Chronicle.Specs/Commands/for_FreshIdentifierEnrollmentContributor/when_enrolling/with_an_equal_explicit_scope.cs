// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentContributor.when_enrolling;

public class with_an_equal_explicit_scope : given.a_contributor
{
    void Establish() => _enrollment = _enrollment with { SuppliedScope = new(EventSequenceNumber.BeforeFirst, EventSourceId: _id, EventStreamType: EventStreamType.All, EventStreamId: EventStreamId.Default, EventSourceType: EventSourceType.Unspecified, EventTypes: []) };
    void Because() => _scope = _contributor.GetRequiredScope(_enrollment);

    [Fact] void should_accept_equivalent_sentinels() => _scope.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_not_record_speculative_provenance() => FreshIdentifierScopes.For(_receivingUnit).ShouldBeNull();
}
