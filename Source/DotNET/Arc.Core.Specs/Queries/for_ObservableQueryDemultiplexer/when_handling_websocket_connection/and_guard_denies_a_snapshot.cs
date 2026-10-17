// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_guard_denies_a_snapshot : given.a_guarded_websocket_connection
{
    void Establish()
    {
        _streamingData = new[] { "secret" };
        _verdict = _ => ObservableQueryEmissionVerdict.DenyAndTerminate;
    }

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_not_send_data() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_identify_the_snapshot() => _guardCalls.Single().IsSnapshot.ShouldBeTrue();
    [Fact] void should_identify_the_first_emission() => _guardCalls.Single().IsFirstEmission.ShouldBeTrue();
    [Fact] void should_supply_coerced_arguments() => _guardCalls.Single().Arguments["id"].ShouldEqual(42);
}
