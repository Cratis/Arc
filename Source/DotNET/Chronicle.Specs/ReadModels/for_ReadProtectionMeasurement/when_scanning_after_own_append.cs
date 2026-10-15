// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Configuration;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_scanning_after_own_append : given.a_measure_pipeline
{
    ReadProtectionMeasureQueue _queue;
    MeterListener _scanListener;
    readonly TaskCompletionSource<string> _match = new(TaskCreationOptions.RunContinuationsAsynchronously);
    string _reason;

    void Establish()
    {
        _queue = new();
        _services.GetService(typeof(ReadProtectionMeasureQueue)).Returns(_queue);
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ReadProtection:MeasureSampleRate"] = "1",
            ["ReadProtection:MeasureTailTimeout"] = "5000"
        }).Build();
        _log.GetTailSequenceNumber((EventSourceId)"subject", null, null, null, Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)102ul);
        var foreign = Metadata(101);
        var own = Metadata(102);
        ((given.MeasuredEventLog)_log).GetMetadataAt(Arg.Any<IEnumerable<EventSequenceNumber>>(), Arg.Any<CancellationToken>())
            .Returns(ImmutableDictionary<EventSequenceNumber, Cratis.Chronicle.EventSequences.EventMetadata>.Empty.Add(101ul, foreign).Add(102ul, own));
        _scanListener = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Cratis.Arc.ReadProtection") listener.EnableMeasurementEvents(instrument);
            }
        };
        _scanListener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            string? command = null;
            string? category = null;
            string? reason = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "command") command = tag.Value as string;
                if (tag.Key == "category") category = tag.Value as string;
                if (tag.Key == "reason") reason = tag.Value as string;
            }
            if (category == "matching_events_since_start" && command == MeasuredCommandType.FullName) _match.TrySetResult(reason!);
        });
        _scanListener.Start();
    }

    async Task Because()
    {
        await _queue.StartAsync(CancellationToken.None);
        _result = await Run(true, context =>
        {
            ReadParameter(context, "stock");
            Stage(context);
            return Task.CompletedTask;
        });
        _reason = await _match.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _queue.StopAsync(CancellationToken.None);
    }

    void Destroy()
    {
        _scanListener.Dispose();
        _queue.Dispose();
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_foreign_101() => _reason.ShouldContain("at 101 would have conflicted");
    [Fact] void should_exclude_own_102_event_by_event() => _telemetry.Where(_ => _.Category == "matching_events_since_start").ShouldNotContain(_ => _.Reason!.Contains("at 102 ", StringComparison.Ordinal));
    [Fact] void should_scan_despite_the_tail_being_our_own_append() => _log.ReceivedCalls().ShouldContain(_ => _.GetMethodInfo().Name == "GetMetadataAt");
}
