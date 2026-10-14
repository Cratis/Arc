// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentContributor.when_enrolling;

public class with_a_disposed_contributor : given.a_contributor
{
    void Because()
    {
        using (var registration = _receivingUnit.AddEnrollmentContributor(_contributor))
        {
            _receivingUnit.AddEvents(EventSequenceId.Log, [new(_id, new Created("first"))], []);
        }
        _receivingUnit.AddEvents(EventSequenceId.Log, [new(_id, new Created("second"))], []);
    }

    [Fact] void should_keep_the_successful_provenance() => FreshIdentifierScopes.For(_receivingUnit)!.Contains(EventSequenceId.Log, _id).ShouldBeTrue();
    [Fact] void should_accept_subsequent_unscoped_events() => _receivingUnit.GetEvents().Count().ShouldEqual(2);
}
