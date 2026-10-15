// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Chronicle.ReadModels.for_ReadProtectionMeasurement;

public class when_the_log_sink_fails : given.a_measure_pipeline
{
    CommandResult _off;
    CommandResult _measure;

    void Establish() => _services.GetService(typeof(ILogger<ReadProtectionMeasurement>)).Returns(new FailingLogger());

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

    [Fact] void should_keep_result_and_status_inputs() => (_measure.IsSuccess, _measure.IsValid, _measure.HasExceptions).ShouldEqual((_off.IsSuccess, _off.IsValid, _off.HasExceptions));
    [Fact] void should_keep_appended_events_and_scopes() => _batches[1].ShouldEqual(_batches[0]);
    [Fact] void should_report_unavailable_through_the_independent_meter() => _telemetry.ShouldContain(_ => _.Category == "measurement_unavailable" && _.Reason == nameof(InvalidOperationException));

    sealed class FailingLogger : ILogger<ReadProtectionMeasurement>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => throw new InvalidOperationException("Log sink failure");
    }
}
