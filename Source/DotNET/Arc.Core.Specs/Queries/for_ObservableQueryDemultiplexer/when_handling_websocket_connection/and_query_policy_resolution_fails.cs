// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_query_policy_resolution_fails : given.a_guarded_websocket_connection
{
    void Establish()
    {
        _principal = new ClaimsPrincipal(new ClaimsIdentity());
        _context.User.Returns(_principal);
        _fallbackAuthorization.IsAuthorized(Arg.Any<IHttpRequestContext>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>()).Returns(false);
    }

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        ConfigureGuestQuery(services);
        var runtime = Substitute.For<IAuthorizationPolicyRuntime>();
        runtime.Resolve(Arg.Any<IReadOnlyList<AuthorizationRequirement>>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<IAuthorizationPolicyResolution>(new PolicyProviderUnavailable()));
        services.AddSingleton(runtime);
    }

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_report_a_preparation_error() => HasErrorFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_not_disguise_the_error_as_a_denial() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_run_the_query() => _correlationIds.ShouldBeEmpty();
    [Fact] void should_not_send_data() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_consult_the_host_fallback() => _fallbackAuthorization.DidNotReceive().IsAuthorized(Arg.Any<IHttpRequestContext>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_register_a_subscription() => _healthTracker.DidNotReceiveWithAnyArgs().RegisterSubscription(default!, default!, default!);

    class PolicyProviderUnavailable : Exception;
}
