// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Observability.for_ObservableQueryTelemetry;

public class when_more_queries_are_subscribed_to_than_the_limit : given.an_observable_query_telemetry
{
    const string Second = "MyApp.Authors.OneAuthor";

    void Establish()
    {
        _cardinalityLimit = 1;
        _knownQueries = [KnownQuery, Second];
    }

    void Because()
    {
        var first = CreateTelemetry(KnownQuery);
        first.Start();
        var second = CreateTelemetry(Second);
        second.Start();
        second.Dispose();
        first.Dispose();
    }

    IEnumerable<string?> Names => Subscriptions.Select(_ => _.Tags[WellKnownTelemetryNames.QueryName] as string);

    [Fact] void should_record_the_query_within_the_limit() => Names.First().ShouldEqual(KnownQuery);
    [Fact] void should_fold_the_query_past_the_limit_into_other() => Names.Skip(1).First().ShouldEqual(WellKnownTelemetryNames.Other);
    [Fact] void should_subtract_each_subscription_under_the_name_it_was_added() => Subscriptions.GroupBy(_ => _.Tags[WellKnownTelemetryNames.QueryName]).ShouldEachConformTo(group => group.Sum(_ => _.Value) == 0);
}
