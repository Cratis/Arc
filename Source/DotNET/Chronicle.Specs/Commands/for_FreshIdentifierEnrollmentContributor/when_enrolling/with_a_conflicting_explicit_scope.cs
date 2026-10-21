// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentContributor.when_enrolling;

public class with_a_conflicting_explicit_scope : given.a_contributor
{
    Exception _error;
    void Establish() => _enrollment = _enrollment with { SuppliedScope = ConcurrencyScope.None };
    void Because() => _error = Catch.Exception(() => _contributor.GetRequiredScope(_enrollment));

    [Fact] void should_reject_the_scope() => _error.ShouldBeOfExactType<ConflictingConcurrencyScopes>();
    [Fact] void should_redact_the_identifier() => _error.Message.ShouldNotContain(_id.Value);
    [Fact] void should_not_record_provenance() => FreshIdentifierScopes.For(_receivingUnit).ShouldBeNull();
}
