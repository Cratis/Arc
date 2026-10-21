// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.when_handling_connection;

public class and_the_guard_denies_an_emission_and_ends_the_stream : given.a_telemetered_client_enumerable_observable
{
    void Establish()
    {
        _emissionGuards.HasGuards.Returns(true);
        _verdict = _ => ObservableQueryEmissionVerdict.DenyAndTerminate;
    }

    async Task Because() => await RunConnection();

    [Fact] void should_not_count_a_push() => _observed.Pushes.ShouldBeEmpty();
    [Fact] void should_not_raise_a_push_span() => _observed.PushSpans.ShouldBeEmpty();
    [Fact] void should_count_the_subscription_up_and_then_down() => _observed.Subscriptions.Select(_ => _.Value).ShouldContainOnly(1, -1);
    [Fact] void should_leave_no_subscription_active() => _observed.ActiveSubscriptions.ShouldEqual(0);
}
