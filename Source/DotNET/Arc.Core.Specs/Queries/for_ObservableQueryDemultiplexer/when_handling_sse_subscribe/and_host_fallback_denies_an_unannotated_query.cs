// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_host_fallback_denies_an_unannotated_query : given.a_guarded_sse_connection
{
    void Establish() => _fallbackAuthorization.IsAuthorized(Arg.Any<IHttpRequestContext>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>()).Returns(false);

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_not_run_the_query() => _correlationIds.ShouldBeEmpty();
    [Fact] void should_not_send_data() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_reject_the_subscribe() => _subscribeStatusCodes[FirstQueryId].ShouldEqual(401);
}
