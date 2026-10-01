// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Cratis.Arc.Observability;

/// <summary>
/// Records the spans raised on one activity source and the measurements taken on one meter while a spec runs.
/// </summary>
/// <remarks>
/// Listens to the given instances only, so specs running in parallel with sources or meters of the same name never
/// see each other's telemetry.
/// </remarks>
public sealed class TelemetryRecorder : IDisposable
{
    readonly object _lock = new();
    readonly List<Activity> _activities = [];
    readonly List<RecordedMeasurement> _measurements = [];
    readonly ActivityListener _activityListener;
    readonly MeterListener _meterListener;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryRecorder"/> class.
    /// </summary>
    /// <param name="source">The <see cref="ActivitySource"/> to record spans from.</param>
    /// <param name="meter">The <see cref="Meter"/> to record measurements from, if any.</param>
    public TelemetryRecorder(ActivitySource source, Meter? meter = null)
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = candidate => ReferenceEquals(candidate, source),
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (_lock)
                {
                    _activities.Add(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (meter is not null && ReferenceEquals(instrument.Meter, meter))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };
        _meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
        _meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
        _meterListener.Start();
    }

    /// <summary>
    /// Gets the spans that have stopped.
    /// </summary>
    public IReadOnlyList<Activity> Activities
    {
        get
        {
            lock (_lock)
            {
                return [.. _activities];
            }
        }
    }

    /// <summary>
    /// Gets the measurements recorded.
    /// </summary>
    public IReadOnlyList<RecordedMeasurement> Measurements
    {
        get
        {
            lock (_lock)
            {
                return [.. _measurements];
            }
        }
    }

    /// <summary>
    /// Gets the single span with the given operation name.
    /// </summary>
    /// <param name="operationName">The operation name of the span.</param>
    /// <returns>The span.</returns>
    public Activity Span(string operationName) => Activities.Single(_ => _.OperationName == operationName);

    /// <summary>
    /// Gets the measurements recorded on an instrument.
    /// </summary>
    /// <param name="instrument">The name of the instrument.</param>
    /// <returns>The measurements.</returns>
    public IEnumerable<RecordedMeasurement> MeasurementsOf(string instrument) => Measurements.Where(_ => _.Instrument == instrument);

    /// <summary>
    /// Determines whether any recorded span, span event or measurement carries the given text anywhere in a tag.
    /// </summary>
    /// <param name="text">The text to look for.</param>
    /// <returns>True if any tag contains it; otherwise false.</returns>
    public bool AnyTagContains(string text)
    {
        var spanTags = Activities.SelectMany(activity => activity.TagObjects.Concat(activity.Events.SelectMany(_ => _.Tags)));
        var measurementTags = Measurements.SelectMany(_ => _.Tags);
        return spanTags.Concat(measurementTags).Any(tag => Describe(tag.Value).Contains(text, StringComparison.Ordinal)) ||
            Activities.Any(_ => _.DisplayName.Contains(text, StringComparison.Ordinal) || (_.StatusDescription ?? string.Empty).Contains(text, StringComparison.Ordinal));
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _activityListener.Dispose();
        _meterListener.Dispose();
    }

    static string Describe(object? value) => value switch
    {
        null => string.Empty,
        IEnumerable<string> values => string.Join(',', values),
        _ => value.ToString() ?? string.Empty
    };

    void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var copied = new Dictionary<string, object?>();
        foreach (var tag in tags)
        {
            copied[tag.Key] = tag.Value;
        }

        lock (_lock)
        {
            _measurements.Add(new(instrument.Name, value, copied));
        }
    }
}
