// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_an_anonymous_subscription_denies_tenant_membership : given.a_guarded_websocket_connection
{
    void Establish()
    {
        _performer.AllowsAnonymousAccess.Returns(true);
        _httpRequestContextAccessor.Current.Returns(_context);
    }

    /// <inheritdoc/>
    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("selected");
        var membership = Substitute.For<ITenantMembership>();
        membership.IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(true, false);
        var verification = new TenantMembershipVerification(_httpRequestContextAccessor, new CurrentPrincipalAccessor(_httpRequestContextAccessor), NullLogger<TenantMembershipVerification>.Instance);
        services.AddSingleton(resolver);
        services.AddSingleton(membership);
        services.AddSingleton(verification);
        services.AddSingleton(new TenantIdAccessor(resolver, verification));
    }

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_report_unauthorized_for_the_subscription() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_not_report_a_generic_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_send_query_data() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
}
