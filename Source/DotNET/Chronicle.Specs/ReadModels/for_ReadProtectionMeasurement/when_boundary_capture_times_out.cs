// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_boundary_capture_times_out : given.a_measure_pipeline
{
    CommandResult _off;
    CommandResult _measure;

    void Establish()
    {
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReadProtection:MeasureTailTimeout"] = "1",
            ["ReadProtection:MeasureSampleRate"] = "1"
        }).Build();
        _log.GetTailSequenceNumber().Returns(new TaskCompletionSource<EventSequenceNumber>().Task);
    }

    async Task Because()
    {
        Task Handle(CommandContext context)
        {
            ReadParameter(context, "stock");
            Stage(context);
            return Task.CompletedTask;
        }
        _off = await Run(false, Handle);
        _measure = await Run(true, Handle);
    }

    [Fact] void should_report_unavailable() => _telemetry.ShouldContain(_ => _.Category == "measurement_unavailable" && _.Reason == nameof(TimeoutException));
    [Fact] void should_keep_the_result_and_status_inputs() => (_measure.IsSuccess, _measure.IsValid, _measure.HasExceptions).ShouldEqual((_off.IsSuccess, _off.IsValid, _off.HasExceptions));
    [Fact] void should_keep_appended_events() => _batches[1].ShouldEqual(_batches[0]);
    [Fact] void should_not_scan() => _log.ReceivedCalls().ShouldNotContain(_ => _.GetMethodInfo().Name == "GetMetadataAt");
}
