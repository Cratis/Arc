// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_streams;

public class and_another_source_uses_the_same_stream : given.a_period_completion
{
    AppendResult _ownerAppend;
    AppendResult _otherAppend;

    async Task Because()
    {
        _result = await _scenario.Execute(new(_id));
        _ownerAppend = await _scenario.EventLog.Append(_id, new PeriodCompleted(), "completion", "period");
        _otherAppend = await _scenario.EventLog.Append("another-owner", new PeriodCompleted(), "completion", "period");
    }

    [Fact] void should_complete_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] Task should_record_the_owner_scope() => _scenario.ShouldHaveCompletedStream(new ClosedStreamScope(EventSourceId: _id, EventStreamType: "completion", EventStreamId: "period"));
    [Fact] void should_refuse_an_append_for_the_closed_owner() => _ownerAppend.IsSuccess.ShouldBeFalse();
    [Fact] void should_leave_the_other_owner_open() => _otherAppend.IsSuccess.ShouldBeTrue();
}
