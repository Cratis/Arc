// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using Cratis.Arc.Commands;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_telemetry_consumers_fail : given.a_measure_pipeline
{
    MeterListener _throwing;
    readonly ConcurrentQueue<string> _logs = new();
    CommandResult _off;
    CommandResult _measure;

    void Establish()
    {
        _services.GetService(typeof(ILogger<ReadProtectionMeasurement>)).Returns(new RecordingLogger(_logs));
        _throwing = new()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Cratis.Arc.ReadProtection") listener.EnableMeasurementEvents(instrument);
            }
        };
        _throwing.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "command" && (tag.Value as string) == MeasuredCommandType.FullName) throw new InvalidOperationException("Telemetry consumer failure");
            }
        });
        _throwing.Start();
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

    void Destroy() => _throwing.Dispose();
    [Fact] void should_keep_the_result() => _measure.IsSuccess.ShouldEqual(_off.IsSuccess);
    [Fact] void should_keep_status_inputs() => (_measure.IsAuthorized, _measure.IsValid, _measure.HasExceptions).ShouldEqual((_off.IsAuthorized, _off.IsValid, _off.HasExceptions));
    [Fact] void should_keep_appended_events_and_scopes() => _batches[1].ShouldEqual(_batches[0]);
    [Fact] void should_report_unavailable_to_the_independent_log_sink() => _logs.ShouldContain(_ => _.Contains("measurement unavailable", StringComparison.Ordinal));

    sealed class RecordingLogger(ConcurrentQueue<string> logs) : ILogger<ReadProtectionMeasurement>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => logs.Enqueue(formatter(state, exception));
    }
}
