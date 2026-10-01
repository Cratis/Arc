// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Arc.Observability;

namespace Cratis.Arc.Validation.for_ValidatorSpans;

public class when_starting_spans : Specification
{
    static readonly Type[] _validatorTypes =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal), typeof(char), typeof(string), typeof(bool), typeof(Guid), typeof(DateTime),
        typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan), typeof(Uri)
    ];

    ActivitySource _source;
    TelemetryRecorder _telemetry;
    ValidatorSpans _spans;
    Activity? _repeated;
    List<Activity?> _started;

    void Establish()
    {
        _source = new ActivitySource("Cratis.Arc.Test");
        _telemetry = new TelemetryRecorder(_source);
        _spans = new(_source);
    }

    void Because()
    {
        using (_spans.Start(typeof(object)))
        {
        }

        _repeated = _spans.Start(typeof(object));
        _started = [.. _validatorTypes.Select(_ =>
        {
            var activity = _spans.Start(_);
            activity?.Dispose();
            return activity;
        })];
    }

    void Destroy()
    {
        _telemetry.Dispose();
        _source.Dispose();
    }

    [Fact] void should_not_start_a_second_span_for_the_same_validator_type() => _repeated.ShouldBeNull();
    [Fact] void should_start_spans_up_to_the_limit() => _telemetry.Activities.Count.ShouldEqual(ValidatorSpans.MaxSpans);
    [Fact] void should_not_start_spans_past_the_limit() => _started.Count(_ => _ is null).ShouldEqual(_validatorTypes.Length + 1 - ValidatorSpans.MaxSpans);
}
