// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentContributor.when_enrolling;

public class with_an_unguarded_capture : given.a_contributor
{
    async Task Establish()
    {
        await Capture<CreatesUnguardedId>();
        _contributor = new(FreshIdentifierScopes.For(_context), _receivingUnit);
    }
    void Because() => _scope = _contributor.GetRequiredScope(_enrollment);

    [Fact] void should_not_require_a_scope() => _scope.ShouldEqual(ConcurrencyScope.NotSet);
    [Fact] void should_not_record_provenance() => FreshIdentifierScopes.For(_receivingUnit).ShouldBeNull();
}
