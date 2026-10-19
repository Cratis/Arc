// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Arc.Queries;
using Cratis.Traces;

namespace Cratis.Arc.Observability;

/// <summary>
/// The services an observable query finds its telemetry through, together with a recorder of what it reports.
/// </summary>
internal sealed class ObservedQueryServices : IDisposable
{
    readonly ActivitySource _source = new("Cratis.Arc.Test");
    readonly Meter _meter = new("Cratis.Arc.Test");

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservedQueryServices"/> class.
    /// </summary>
    /// <param name="knownQueries">The fully qualified names of the queries that exist.</param>
    /// <param name="cardinalityLimit">The number of distinct query names to record.</param>
    public ObservedQueryServices(IEnumerable<string> knownQueries, int cardinalityLimit = PipelineMetrics.DefaultCardinalityLimit)
    {
        Telemetry = new(_source, _meter);

        var activitySource = Substitute.For<IActivitySource<QueryPipeline>>();
        activitySource.ActualSource.Returns(_source);

        KnownQueries = [.. knownQueries];
        var providers = Substitute.For<IQueryPerformerProviders>();
        providers.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer?>()).Returns(callInfo =>
        {
            callInfo[1] = Substitute.For<IQueryPerformer>();
            return KnownQueries.Contains(callInfo.Arg<FullyQualifiedQueryName>().Value);
        });

        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(PipelineMetrics)).Returns(new PipelineMetrics(_meter, cardinalityLimit));
        services.GetService(typeof(IActivitySource<QueryPipeline>)).Returns(activitySource);
        services.GetService(typeof(IQueryPerformerProviders)).Returns(providers);
        Services = services;
    }

    /// <summary>
    /// Gets the fully qualified names of the queries that exist. Remove from it to make a query unknown.
    /// </summary>
    public List<string> KnownQueries { get; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> to hand an observable query as its request services.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Gets the <see cref="TelemetryRecorder"/> recording what the observable query reports.
    /// </summary>
    public TelemetryRecorder Telemetry { get; }

    /// <summary>
    /// Gets the changes to the active subscriptions count, in the order they were recorded.
    /// </summary>
    public IEnumerable<RecordedMeasurement> Subscriptions => Telemetry.MeasurementsOf(WellKnownTelemetryNames.QuerySubscriptionsMetric);

    /// <summary>
    /// Gets the pushes counted.
    /// </summary>
    public IEnumerable<RecordedMeasurement> Pushes => Telemetry.MeasurementsOf(WellKnownTelemetryNames.QueryPushesMetric);

    /// <summary>
    /// Gets the spans raised for pushes.
    /// </summary>
    public IEnumerable<Activity> PushSpans => Telemetry.Activities.Where(_ => _.OperationName == WellKnownTelemetryNames.QueryPushSpan);

    /// <summary>
    /// Gets the sum of every change recorded to the active subscriptions count.
    /// </summary>
    public long ActiveSubscriptions => (long)Subscriptions.Sum(_ => _.Value);

    /// <inheritdoc/>
    public void Dispose()
    {
        Telemetry.Dispose();
        _meter.Dispose();
        _source.Dispose();
    }
}
