// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.given;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.given;

public class a_refusal_with_delivery : a_refused_invocation
{
    protected IdempotentEventLog _trackedLog;
    protected object _branch = new RefusalRecorded("refused");

    void Establish()
    {
        _trackedLog = Substitute.For<IdempotentEventLog>();
        _trackedLog.SupportsIdempotentAppends.Returns(true);
        _trackedLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>()).Returns(call =>
        {
            _appended = call.Arg<IEnumerable<EventForEventSourceId>>().ToArray();
            return _trackedLog.Record(_appended);
        });
        _eventLog = _trackedLog;
        _store.EventLog.Returns(_eventLog);
        _context = _context with { Delivery = new ReactorDelivery("reactor", "store", "namespace", EventSequenceId.Log, "observed", 42) };
        _invocation = Invoke.Command(_command).OnRefused(Refused.ByValidation, _ => _branch);
    }
}
