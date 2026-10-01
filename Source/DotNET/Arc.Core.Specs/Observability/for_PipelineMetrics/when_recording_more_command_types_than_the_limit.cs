// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Cratis.Arc.Observability.for_PipelineMetrics;

public class when_recording_more_command_types_than_the_limit : Specification
{
    ActivitySource _source;
    Meter _meter;
    TelemetryRecorder _telemetry;
    PipelineMetrics _metrics;

    void Establish()
    {
        _source = new("Cratis.Arc.Test");
        _meter = new("Cratis.Arc.Test");
        _telemetry = new(_source, _meter);
        _metrics = new(_meter, cardinalityLimit: 1);
    }

    void Destroy()
    {
        _telemetry.Dispose();
        _meter.Dispose();
        _source.Dispose();
    }

    void Because()
    {
        _metrics.RecordCommand("RegisterAuthor", WellKnownOperationOutcomes.Success, TimeSpan.FromMilliseconds(250));
        _metrics.RecordCommand("RenameAuthor", WellKnownOperationOutcomes.Validation, TimeSpan.FromMilliseconds(10));
    }

    IEnumerable<string?> RecordedTypes => _telemetry.MeasurementsOf(WellKnownTelemetryNames.CommandDurationMetric).Select(_ => _.Tags[WellKnownTelemetryNames.CommandType] as string);

    [Fact] void should_record_the_first_type() => RecordedTypes.First().ShouldEqual("RegisterAuthor");
    [Fact] void should_fold_the_type_past_the_limit_into_other() => RecordedTypes.Last().ShouldEqual(WellKnownTelemetryNames.Other);
    [Fact] void should_record_the_duration_in_seconds() => _telemetry.MeasurementsOf(WellKnownTelemetryNames.CommandDurationMetric).First().Value.ShouldEqual(0.25);
    [Fact] void should_count_each_outcome() => _telemetry.MeasurementsOf(WellKnownTelemetryNames.CommandOutcomesMetric).Select(_ => _.Tags[WellKnownTelemetryNames.CommandOutcome]).ShouldContainOnly(WellKnownOperationOutcomes.Success, WellKnownOperationOutcomes.Validation);
}
