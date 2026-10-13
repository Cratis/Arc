// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.given;

public class a_generated_target_collision : a_generated_target
{
    void Establish()
    {
        var violation = new ConcurrencyViolation(_id, EventSequenceNumber.BeforeFirst, EventSequenceNumber.First);
        _eventLog.Append(
            _id,
            Arg.Any<object>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<ConcurrencyScope?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Subject?>())
            .Returns(call =>
            {
                _scope = call.Arg<ConcurrencyScope>();
                return AppendResult.Failed(_context.CorrelationId, violation);
            });
        _eventLog.AppendMany(
            _id,
            Arg.Any<IEnumerable<object>>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<ConcurrencyScope?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Subject?>())
            .Returns(call =>
            {
                _scope = call.Arg<ConcurrencyScope>();
                return AppendManyResult.Failed(_context.CorrelationId, [violation]);
            });
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(call =>
            {
                _scope = call.Arg<IDictionary<EventSourceId, ConcurrencyScope>>()[_id];
                return AppendManyResult.Failed(_context.CorrelationId, [violation]);
            });
    }
}
