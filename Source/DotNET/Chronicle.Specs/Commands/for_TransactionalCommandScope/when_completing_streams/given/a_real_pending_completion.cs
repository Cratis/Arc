// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams.given;

public class a_real_pending_completion : a_pending_completion
{
    protected UnitOfWork _realUnitOfWork;

    void Establish()
    {
        var store = Substitute.For<IEventStore>();
        store.GetEventSequence(EventSequenceId.Log).Returns(_eventLog);
        _realUnitOfWork = new(_correlationId, _ => { }, store);
        _unitOfWorkManager.Begin(_correlationId).Returns(_realUnitOfWork);
    }
}
