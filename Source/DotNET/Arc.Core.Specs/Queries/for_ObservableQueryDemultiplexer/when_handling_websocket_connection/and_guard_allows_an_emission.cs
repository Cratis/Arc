// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

/// <summary>
/// The client sends its arguments as strings, because that is all a query string can carry; the pipeline coerces them
/// to the declared parameter types and publishes them on the query context. A guard has to be told those, not the
/// unconverted strings that came in over the wire — see <c>should_tell_the_guard_the_coerced_arguments</c>.
/// </summary>
public class and_guard_allows_an_emission : given.a_guarded_websocket_connection
{
    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["event-store-a"]);
        await WaitFor(() => CountQueryResultsFor(FirstQueryId) == 1);
    });

    [Fact] void should_consult_the_guard() => _guardCalls.Count.ShouldEqual(1);
    [Fact] void should_write_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
    [Fact] void should_not_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_tell_the_guard_the_query_name() => _guardCalls.First().QueryName.Value.ShouldEqual(QueryName);
    [Fact] void should_tell_the_guard_the_caller_identity() => _guardCalls.First().Principal.ShouldEqual(_principal);
    [Fact] void should_tell_the_guard_it_is_the_first_emission() => _guardCalls.First().IsFirstEmission.ShouldBeTrue();
    [Fact] void should_tell_the_guard_the_coerced_arguments() => _guardCalls.First().Arguments["id"].ShouldEqual(42);
}
