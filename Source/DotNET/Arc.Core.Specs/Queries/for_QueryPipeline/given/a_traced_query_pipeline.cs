// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Cratis.Arc.Observability;

namespace Cratis.Arc.Queries.for_QueryPipeline.given;

public class a_traced_query_pipeline : a_query_pipeline
{
    protected FullyQualifiedQueryName _queryName;
    protected TelemetryRecorder _telemetry;
    protected QueryResult _result;
    Meter _meter;

    protected Activity QuerySpan => _telemetry.Span("cratis.arc.query.perform");

    protected RecordedMeasurement Duration => _telemetry.MeasurementsOf("cratis.arc.query.duration").Single();

    void Establish()
    {
        _queryName = "MyApp.Authors.AllAuthors";
        _queryPerformer.Name.Returns((QueryName)"AllAuthors");
        _queryPerformer.FullyQualifiedName.Returns(_queryName);
        _queryPerformer.ReadModelType.Returns(typeof(object));
        _queryPerformer.Dependencies.Returns(new List<Type>());
        query_filters.OnPerform(Arg.Any<QueryContext>()).Returns(QueryResult.Success(_correlationId));

        _meter = new Meter("Cratis.Arc.Test");
        _serviceProvider.GetService(typeof(PipelineMetrics)).Returns(new PipelineMetrics(_meter));
        _telemetry = new TelemetryRecorder(_activitySource, _meter);
    }

    void Destroy()
    {
        _telemetry.Dispose();
        _meter.Dispose();
    }

    protected void WithKnownQuery() =>
        _queryPerformerProviders.TryGetPerformersFor(_queryName, out var _).Returns(callInfo =>
        {
            callInfo[1] = _queryPerformer;
            return true;
        });

    protected async Task Perform() => _result = await _pipeline.Perform(_queryName, new(), Paging.NotPaged, Sorting.None, _serviceProvider);
}
