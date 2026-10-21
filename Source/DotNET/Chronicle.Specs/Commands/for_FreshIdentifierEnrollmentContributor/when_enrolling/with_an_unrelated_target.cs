// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentContributor.when_enrolling;

public class with_an_unrelated_target : given.a_contributor
{
    void Establish() => _enrollment = _enrollment with { EventSourceId = EventSourceId.New(), SuppliedScope = ConcurrencyScope.None };
    void Because()
    {
        _scope = _contributor.GetRequiredScope(_enrollment);
        _contributor.Enrolled(_enrollment, _enrollment.SuppliedScope);
    }

    [Fact] void should_leave_the_callers_scope_alone() => _scope.ShouldEqual(ConcurrencyScope.NotSet);
    [Fact] void should_not_record_provenance() => FreshIdentifierScopes.For(_receivingUnit).ShouldBeNull();
}
