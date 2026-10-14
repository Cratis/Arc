// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_FreshIdentifierEnrollmentContributor.given;

public class a_contributor : Commands.given.a_generated_target
{
    internal FreshIdentifierEnrollmentContributor _contributor;
    protected UnitOfWork _receivingUnit;
    protected UnitOfWorkEnrollment _enrollment;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        store.GetEventSequence(EventSequenceId.Log).Returns(_eventLog);
        _receivingUnit = new(_context.CorrelationId, _ => { }, store);
        _contributor = new(_freshness, _receivingUnit);
        _enrollment = new(EventSequenceId.Log, _id, ConcurrencyScope.NotSet, [new Created("direct")]);
    }
}
