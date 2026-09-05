// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

public class and_guard_allows_an_emission : given.a_guarded_client_observable_sse
{
    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext("event-store-a");
        await WaitFor(() => _messages.Count == 1);
    });

    [Fact] void should_consult_the_guard() => _guardCalls.Count.ShouldEqual(1);
    [Fact] void should_write_the_emission_unchanged() => WrittenResults.Single().Data.ToString().ShouldEqual("event-store-a");
    [Fact] void should_write_it_as_authorized() => WrittenResults.Single().IsAuthorized.ShouldBeTrue();
    [Fact] void should_tell_the_guard_the_query_name() => _guardCalls.Single().QueryName.Value.ShouldEqual(QueryName);
    [Fact] void should_tell_the_guard_the_caller_identity() => _guardCalls.Single().Principal.ShouldEqual(_principal);
}
