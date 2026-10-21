// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_host_fallback_admits_an_allow_anonymous_query : given.a_guarded_websocket_connection
{
    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity());
        _context.User.Returns(_principal);
        _performer.AllowsAnonymousAccess.Returns(true);
        _fallbackAuthorization.IsAuthorized(Arg.Any<IHttpRequestContext>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>()).Returns(false);
        _streamingData = new[] { "snapshot" };
    }

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_send_the_result() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
    [Fact] void should_not_consult_the_host_fallback() => _fallbackAuthorization.DidNotReceive().IsAuthorized(Arg.Any<IHttpRequestContext>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
}
