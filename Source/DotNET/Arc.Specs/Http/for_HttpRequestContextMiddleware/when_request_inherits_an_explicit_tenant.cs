// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Http.for_HttpRequestContextMiddleware;

public class when_request_inherits_an_explicit_tenant
{
    [Fact]
    public async Task should_resolve_from_the_request_and_honor_a_scope_inside_it()
    {
        var requests = new HttpRequestContextAccessor();
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns(_ => requests.Current?.Headers.GetValueOrDefault("X-Tenant") ?? string.Empty);
        var tenants = new TenantIdAccessor(resolver);
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant"] = "tenant-B";
        var middleware = new HttpRequestContextMiddleware(requests);
        using (tenants.Begin("tenant-A"))
        {
            await middleware.InvokeAsync(context, _ =>
            {
                tenants.Current.ShouldEqual(new TenantId("tenant-B"));
                using (tenants.Begin("inside"))
                {
                    tenants.Current.ShouldEqual(new TenantId("inside"));
                }
                tenants.Current.ShouldEqual(new TenantId("tenant-B"));
                return Task.CompletedTask;
            });
            tenants.Current.ShouldEqual(new TenantId("tenant-A"));
        }
    }
}
