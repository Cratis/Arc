// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_QueryPipeline.when_performing;

public class with_a_traced_observable_query : given.a_traced_query_pipeline
{
    void Establish()
    {
        WithKnownQuery();
        var subject = new ReplaySubject<IEnumerable<object>>();
        _queryPerformer.Perform(Arg.Any<QueryContext>()).Returns(ValueTask.FromResult<object?>(subject));
        _queryRenderers.Render(_queryName, subject, _serviceProvider).Returns(new QueryRendererResult(0, subject));
    }

    Task Because() => Perform();

    [Fact] void should_add_the_observable_transport() => QuerySpan.GetTagItem(WellKnownTelemetryNames.QueryTransport).ShouldEqual(WellKnownTelemetryNames.ObservableTransport);
    [Fact] void should_record_the_duration_for_the_transport() => Duration.Tags[WellKnownTelemetryNames.QueryTransport].ShouldEqual(WellKnownTelemetryNames.ObservableTransport);
}
