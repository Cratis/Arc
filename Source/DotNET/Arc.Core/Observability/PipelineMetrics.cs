// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Cratis.Arc.Observability;

/// <summary>
/// Records the command and query pipeline metrics on Arc's meter.
/// </summary>
internal sealed class PipelineMetrics
{
    /// <summary>
    /// The number of distinct command types and query names recorded before further ones are folded into
    /// <see cref="WellKnownTelemetryNames.Other"/>.
    /// </summary>
    internal const int DefaultCardinalityLimit = 1000;

#if NET9_0_OR_GREATER
    static readonly double[] _durationBuckets = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10];
#endif

    readonly Histogram<double> _commandDuration;
    readonly Counter<long> _commandOutcomes;
    readonly Histogram<double> _queryDuration;
    readonly CardinalityLimiter _commandTypes;
    readonly CardinalityLimiter _queryNames;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineMetrics"/> class.
    /// </summary>
    /// <param name="meter">The <see cref="Meter"/> to create the instruments on.</param>
    /// <param name="cardinalityLimit">The number of distinct command types and query names to record.</param>
    internal PipelineMetrics(Meter meter, int cardinalityLimit = DefaultCardinalityLimit)
    {
        _commandDuration = CreateDurationHistogram(meter, WellKnownTelemetryNames.CommandDurationMetric, "How long commands take to run through the command pipeline.");
        _commandOutcomes = meter.CreateCounter<long>(WellKnownTelemetryNames.CommandOutcomesMetric, "{command}", "The number of commands run, by outcome.");
        _queryDuration = CreateDurationHistogram(meter, WellKnownTelemetryNames.QueryDurationMetric, "How long queries take to run through the query pipeline.");
        _commandTypes = new(cardinalityLimit);
        _queryNames = new(cardinalityLimit);
    }

    /// <summary>
    /// Gets a value indicating whether anything listens to the command metrics.
    /// </summary>
    internal bool CommandsEnabled => _commandDuration.Enabled || _commandOutcomes.Enabled;

    /// <summary>
    /// Gets a value indicating whether anything listens to the query metrics.
    /// </summary>
    internal bool QueriesEnabled => _queryDuration.Enabled;

    /// <summary>
    /// Records that a command ran.
    /// </summary>
    /// <param name="commandType">The full name of the command type.</param>
    /// <param name="outcome">The outcome, one of the <see cref="WellKnownOperationOutcomes"/>.</param>
    /// <param name="elapsed">How long the command took.</param>
    internal void RecordCommand(string commandType, string outcome, TimeSpan elapsed)
    {
        if (!CommandsEnabled)
        {
            return;
        }

        var tags = new TagList
        {
            { WellKnownTelemetryNames.CommandType, _commandTypes.Limit(commandType) },
            { WellKnownTelemetryNames.CommandOutcome, outcome }
        };
        _commandDuration.Record(elapsed.TotalSeconds, tags);
        _commandOutcomes.Add(1, tags);
    }

    /// <summary>
    /// Records that a query ran.
    /// </summary>
    /// <param name="queryName">The fully qualified name of the query, or <see cref="WellKnownTelemetryNames.Other"/> for a query that is not known.</param>
    /// <param name="transport">The transport the result is delivered over.</param>
    /// <param name="outcome">The outcome, one of the <see cref="WellKnownOperationOutcomes"/>.</param>
    /// <param name="elapsed">How long the query took.</param>
    internal void RecordQuery(string queryName, string transport, string outcome, TimeSpan elapsed)
    {
        if (!QueriesEnabled)
        {
            return;
        }

        var tags = new TagList
        {
            { WellKnownTelemetryNames.QueryName, _queryNames.Limit(queryName) },
            { WellKnownTelemetryNames.QueryTransport, transport },
            { WellKnownTelemetryNames.QueryOutcome, outcome }
        };
        _queryDuration.Record(elapsed.TotalSeconds, tags);
    }

    static Histogram<double> CreateDurationHistogram(Meter meter, string name, string description) =>
#if NET9_0_OR_GREATER
        meter.CreateHistogram(name, "s", description, tags: null, advice: new InstrumentAdvice<double> { HistogramBucketBoundaries = _durationBuckets });
#else
        meter.CreateHistogram<double>(name, "s", description);
#endif
}
