// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_the_handler_appends_immediately : given.a_measure_pipeline
{
    CommandResult _off;
    CommandResult _measure;
    readonly List<object> _immediate = [];

    void Establish() => _log.Append("subject", Arg.Any<object>()).Returns(call =>
    {
        var content = call.ArgAt<object>(1);
        var result = AppendResult.Success(_unit.CorrelationId, 99ul);
        _immediate.Add(content);
        _appends.OnNext([new(new(EventContext.Empty with { EventSourceId = "subject", SequenceNumber = 99ul, EventType = _eventType }, content), result)]);
        return result;
    });

    async Task Because()
    {
        async Task Handle(CommandContext context)
        {
            ReadParameter(context, "stock");
            var append = await _log.Append("subject", new Fact("immediate"));
            append.IsSuccess.ShouldBeTrue();
            Stage(context);
        }
        _off = await Run(false, Handle);
        _measure = await Run(true, Handle);
    }

    [Fact] void should_keep_the_result_and_status_inputs() => (_measure.IsSuccess, _measure.IsValid, _measure.HasExceptions).ShouldEqual((_off.IsSuccess, _off.IsValid, _off.HasExceptions));
    [Fact] void should_keep_the_immediate_events() => _immediate.ShouldEqual(new object[] { new Fact("immediate"), new Fact("immediate") });
    [Fact] void should_keep_the_transactional_events() => _batches[1].ShouldEqual(_batches[0]);
    [Fact] void should_report_immediate_appends() => _telemetry.Select(_ => _.Category).ShouldContain("immediate_append");
}
