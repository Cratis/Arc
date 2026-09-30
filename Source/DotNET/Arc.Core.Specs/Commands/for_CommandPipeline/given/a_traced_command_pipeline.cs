// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Arc.Observability;

namespace Cratis.Arc.Commands.for_CommandPipeline.given;

public class a_traced_command_pipeline : a_command_pipeline
{
    protected const string SecretName = "a name that must never reach telemetry";

    protected RegisterAuthor _command;
    protected ICommandHandler _commandHandler;
    protected TelemetryRecorder _telemetry;
    protected CommandResult _result;
    Meter _meter;

    protected Activity CommandSpan => _telemetry.Span("cratis.arc.command.execute");

    protected IEnumerable<RecordedMeasurement> Durations => _telemetry.MeasurementsOf("cratis.arc.command.duration");

    protected IEnumerable<RecordedMeasurement> Outcomes => _telemetry.MeasurementsOf("cratis.arc.command.outcomes");

    void Establish()
    {
        _command = new(SecretName);
        _commandHandler = Substitute.For<ICommandHandler>();
        var anyHandler = Arg.Any<ICommandHandler>();
        _commandHandlerProviders
            .TryGetHandlerFor(_command, out anyHandler)
            .Returns(r =>
            {
                r[1] = _commandHandler;
                return true;
            });

        _meter = new Meter("Cratis.Arc.Test");
        _serviceProvider.GetService(typeof(PipelineMetrics)).Returns(new PipelineMetrics(_meter));
        _telemetry = new TelemetryRecorder(_activitySource, _meter);
    }

    void Destroy()
    {
        _telemetry.Dispose();
        _meter.Dispose();
    }

    public record RegisterAuthor(string Name);
}
