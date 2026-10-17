// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_host_fallback_throws : given.a_guarded_sse_connection
{
    void Establish() => _fallbackAuthorization.IsAuthorized(Arg.Any<IHttpRequestContext>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>())
        .Returns(Task.FromException<bool>(new InvalidAuthorizationConfiguration("Policy unavailable")));

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_fail_closed() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_not_run_the_query() => _correlationIds.ShouldBeEmpty();
    [Fact] void should_not_send_data() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_reject_the_subscribe() => _subscribeStatusCodes[FirstQueryId].ShouldEqual(401);
}
