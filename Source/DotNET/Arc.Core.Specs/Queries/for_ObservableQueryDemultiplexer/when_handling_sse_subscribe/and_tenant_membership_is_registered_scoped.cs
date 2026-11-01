// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_tenant_membership_is_registered_scoped : given.a_scoped_subscription_container
{
    int _constructed;
    int _disposed;
    Exception _afterEnd;

    void Establish()
    {
        _queryIds = [FirstQueryId, SecondQueryId];
        _performResult = _ =>
        {
            _guardedServiceProvider.GetRequiredService<TenantMembershipVerification>().Verify("tenant");
            return Task.FromResult(_result());
        };
    }

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        base.ConfigureGuards(services, guardTypes);
        services.AddScoped<ITenantMembership>(_ => new CountingMembership(this));
        services.AddSingleton(_ => new TenantMembershipVerification(_httpRequestContextAccessor, Substitute.For<ICurrentPrincipalAccessor>(), NullLogger<TenantMembershipVerification>.Instance));
    }

    async Task Because() => await RunConnection(async () =>
    {
        var verification = _guardedServiceProvider.GetRequiredService<TenantMembershipVerification>();
        var first = _performContexts.First();

        // A read through the first context once its subscription is over must fail closed.
        await _hub.HandleSSEUnsubscribe(CreateUnsubscribeContext(FirstQueryId));
        await WaitFor(() => _resolvedDuringPerform.First().Disposals > 0);
        var previous = _httpRequestContextAccessor.Current;
        _httpRequestContextAccessor.Current = first;
        _afterEnd = Catch.Exception(Read);
        _httpRequestContextAccessor.Current = previous;

        void Read() => verification.Verify("tenant");
    });

    [Fact] void should_construct_one_membership_per_subscription() => _constructed.ShouldEqual(2);
    [Fact] void should_dispose_each_membership_with_its_subscription() => _disposed.ShouldEqual(2);
    [Fact] void should_deny_a_read_after_the_subscription_ended() => _afterEnd.ShouldBeOfExactType<TenantMembershipDenied>();

    sealed class CountingMembership : ITenantMembership, IDisposable
    {
        readonly and_tenant_membership_is_registered_scoped _owner;

        public CountingMembership(and_tenant_membership_is_registered_scoped owner)
        {
            _owner = owner;
            Interlocked.Increment(ref owner._constructed);
        }

        public bool IsMember(ClaimsPrincipal principal, TenantId tenantId) => true;

        public void Dispose() => Interlocked.Increment(ref _owner._disposed);
    }
}
