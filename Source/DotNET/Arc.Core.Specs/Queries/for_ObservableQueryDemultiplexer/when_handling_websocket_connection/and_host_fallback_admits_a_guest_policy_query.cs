// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_host_fallback_admits_a_guest_policy_query : given.a_guarded_websocket_connection
{
    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity());
        _context.User.Returns(_principal);

        // This spec stubs pipeline authorization; the ASP.NET host spec exercises the actual guest policy.
        _context.RequestServices.Returns(Substitute.For<IServiceProvider>());
        _fallbackAuthorization.IsAuthorized(Arg.Any<IHttpRequestContext>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes) => ConfigureGuestQuery(services);

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_reach_the_query_pipeline() => _correlationIds.Count.ShouldEqual(1);
    [Fact] void should_not_consult_the_host_fallback() => _fallbackAuthorization.DidNotReceive().IsAuthorized(Arg.Any<IHttpRequestContext>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_signal_an_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
}
