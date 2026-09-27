// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableEmissionIdentity;

public class when_a_foreign_producer_has_an_explicit_tenant
{
    [Fact]
    public void should_restore_the_subscriber_and_then_the_producer()
    {
        var requests = new HttpRequestContextAccessor();
        var principals = new CurrentPrincipalAccessor(requests);
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("resolved");
        var tenants = new TenantIdAccessor(resolver);
        using var services = new ServiceCollection()
            .AddSingleton<IHttpRequestContextAccessor>(requests)
            .AddSingleton(principals)
            .AddSingleton(tenants)
            .BuildServiceProvider();
        var subscriber = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "subscriber")], "test"));
        var context = Substitute.For<IHttpRequestContext>();
        context.User.Returns(subscriber);
        context.RequestServices.Returns(services);

        using (tenants.Begin("tenant-C"))
        {
            using (ObservableEmissionIdentity.Begin(context, services, subscriber, "tenant-A"))
            {
                tenants.Current.ShouldEqual(new TenantId("tenant-A"));
                principals.Current!.Identity!.Name.ShouldEqual("subscriber");
                using (tenants.Begin("tenant-B"))
                {
                    tenants.Current.ShouldEqual(new TenantId("tenant-B"));
                }
                tenants.Current.ShouldEqual(new TenantId("tenant-A"));
            }
            tenants.Current.ShouldEqual(new TenantId("tenant-C"));
        }
        tenants.Current.ShouldEqual(new TenantId("resolved"));
    }
}
