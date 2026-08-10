// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientEnumerableObservableSSE.when_handling_connection;

public class and_guard_allows_an_emission : given.a_guarded_client_enumerable_observable_sse
{
    async Task Because() => await RunConnection();

    [Fact] void should_consult_the_guard_for_every_item() => _guardCalls.Count.ShouldEqual(2);
    [Fact] void should_write_every_item() => WrittenResults.Count().ShouldEqual(2);
    [Fact] void should_write_the_first_item_unchanged() => WrittenResults.First().Data.ToString().ShouldEqual("event-store-a");
    [Fact] void should_write_them_all_as_authorized() => WrittenResults.Count(_ => _.IsAuthorized).ShouldEqual(2);
    [Fact] void should_tell_the_guard_the_query_name() => _guardCalls.First().QueryName.Value.ShouldEqual(QueryName);
    [Fact] void should_tell_the_guard_the_caller_identity() => _guardCalls.First().Principal.ShouldEqual(_principal);
}
