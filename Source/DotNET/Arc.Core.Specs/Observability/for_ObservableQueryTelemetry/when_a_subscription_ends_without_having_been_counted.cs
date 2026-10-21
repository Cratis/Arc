// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Observability.for_ObservableQueryTelemetry;

public class when_a_subscription_ends_without_having_been_counted : given.an_observable_query_telemetry
{
    void Because() => CreateTelemetry(KnownQuery).Dispose();

    [Fact] void should_not_count_it_down() => Subscriptions.ShouldBeEmpty();
}
