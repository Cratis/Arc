// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CompleteStreamResultHandler.given;

public class a_stream_completion : Specification
{
    protected CompleteStreamResultHandler _handler;
    protected IEventStore _store;
    protected IEventLog _eventLog;
    protected ReactorContext _context;
    protected Result<ReactorSideEffectFailure> _result;

    void Establish()
    {
        _handler = new();
        _store = Substitute.For<IEventStore>();
        _eventLog = Substitute.For<IEventLog>();
        _store.EventLog.Returns(_eventLog);
        _context = new(EventContext.Empty with { EventStreamType = "observed", EventStreamId = "stream" }, new object(), new());
        Result<EventSequenceNumber, CompleteStreamError> completed = EventSequenceNumber.First;
        _eventLog.CompleteStream("observed", "stream").Returns(completed);
    }
}
