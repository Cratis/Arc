// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandScenario.when_completing_streams;

public class and_the_command_has_no_event_source : Specification
{
    CommandScenario<CompleteAllPeriods> _scenario;
    CommandResult _result;
    AppendResult _firstAppend;
    AppendResult _secondAppend;

    async Task Establish()
    {
        _scenario = new();
        await _scenario.EventLog.Append("first-owner", new PeriodCompleted(), "completion", "period");
        await _scenario.EventLog.Append("second-owner", new PeriodCompleted(), "completion", "period");
    }

    async Task Because()
    {
        _result = await _scenario.Execute(new());
        _firstAppend = await _scenario.EventLog.Append("first-owner", new PeriodCompleted(), "completion", "period");
        _secondAppend = await _scenario.EventLog.Append("second-owner", new PeriodCompleted(), "completion", "period");
    }

    [Fact] void should_complete_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] Task should_record_the_stream_completion() => _scenario.ShouldHaveCompletedStream("completion", "period");
    [Fact] void should_refuse_an_append_for_the_first_owner() => _firstAppend.IsSuccess.ShouldBeFalse();
    [Fact] void should_refuse_an_append_for_the_second_owner() => _secondAppend.IsSuccess.ShouldBeFalse();

    void Destroy() => _scenario.Dispose();

    [Command]
    [EventStreamType("completion")]
    [EventStreamId("period")]
    public record CompleteAllPeriods
    {
        public CompleteStream Handle() => new();
    }
}
