// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

public class and_a_result_is_pushed : given.a_telemetered_client_observable_sse
{
    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext("event-store-a");
        await WaitFor(() => _messages.Count == 1);
    });

    [Fact] void should_count_one_push() => _observed.Pushes.Select(_ => _.Value).ShouldContainOnly(1);
    [Fact] void should_tag_the_push_with_the_query_name() => _observed.Pushes.Single().Tags[WellKnownTelemetryNames.QueryName].ShouldEqual(QueryName);
    [Fact] void should_tag_the_push_with_the_transport() => _observed.Pushes.Single().Tags[WellKnownTelemetryNames.QueryTransport].ShouldEqual(WellKnownTelemetryNames.ServerSentEventsTransport);
    [Fact] void should_raise_one_push_span() => _observed.PushSpans.Count().ShouldEqual(1);
    [Fact] void should_tag_the_span_with_the_query_name() => _observed.PushSpans.Single().GetTagItem(WellKnownTelemetryNames.QueryName).ShouldEqual(QueryName);
}
