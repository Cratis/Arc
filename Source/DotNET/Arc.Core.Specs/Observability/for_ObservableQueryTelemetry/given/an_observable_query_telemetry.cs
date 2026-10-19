// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Arc.Queries;
using Cratis.Traces;

namespace Cratis.Arc.Observability.for_ObservableQueryTelemetry.given;

public class an_observable_query_telemetry : Specification
{
    protected const string KnownQuery = "MyApp.Authors.AllAuthors";
    protected const string Transport = WellKnownTelemetryNames.WebSocketTransport;

    protected ActivitySource _source;
    protected Meter _meter;
    protected TelemetryRecorder _telemetry;
    protected IServiceProvider _services;
    protected int _cardinalityLimit = PipelineMetrics.DefaultCardinalityLimit;
    internal PipelineMetrics _metrics;
    protected string[] _knownQueries = [KnownQuery];

    protected IEnumerable<RecordedMeasurement> Subscriptions => _telemetry.MeasurementsOf(WellKnownTelemetryNames.QuerySubscriptionsMetric);
    protected IEnumerable<RecordedMeasurement> Pushes => _telemetry.MeasurementsOf(WellKnownTelemetryNames.QueryPushesMetric);

    void Establish()
    {
        _source = new("Cratis.Arc.Test");
        _meter = new("Cratis.Arc.Test");
        _telemetry = new(_source, _meter);
    }

    void Destroy()
    {
        _telemetry.Dispose();
        _meter.Dispose();
        _source.Dispose();
    }

    internal ObservableQueryTelemetry CreateTelemetry(string queryName)
    {
        var activitySource = Substitute.For<IActivitySource<QueryPipeline>>();
        activitySource.ActualSource.Returns(_source);

        var providers = Substitute.For<IQueryPerformerProviders>();
        providers.TryGetPerformersFor(Arg.Any<FullyQualifiedQueryName>(), out Arg.Any<IQueryPerformer?>()).Returns(callInfo =>
        {
            callInfo[1] = Substitute.For<IQueryPerformer>();
            return _knownQueries.Contains(callInfo.Arg<FullyQualifiedQueryName>().Value);
        });

        var services = Substitute.For<IServiceProvider>();
        _metrics ??= new PipelineMetrics(_meter, _cardinalityLimit);
        services.GetService(typeof(PipelineMetrics)).Returns(_metrics);
        services.GetService(typeof(IActivitySource<QueryPipeline>)).Returns(activitySource);
        services.GetService(typeof(IQueryPerformerProviders)).Returns(providers);
        _services = services;

        return new ObservableQueryTelemetry(services, queryName, Transport);
    }
}
