// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper;

public class when_a_listener_is_started_inside_a_tenant_scope : given.a_running_endpoint_mapper
{
    TenantIdAccessor _tenants;
    string _requestTenant;
    string _insideTenant;
    string _restoredTenant;

    void Establish()
    {
        var services = new ServiceCollection();
        var requests = new HttpRequestContextAccessor();
        services.AddSingleton<IHttpRequestContextAccessor>(requests);
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns(_ => requests.Current?.Headers.GetValueOrDefault("X-Tenant") ?? string.Empty);
        services.AddSingleton(resolver);
        _tenants = new TenantIdAccessor(resolver);
        services.AddSingleton(_tenants);
        services.AddSingleton<IAuthentication>(Substitute.For<IAuthentication>());
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();

        _endpointMapper.MapGet("/tenant", async context =>
        {
            _requestTenant = _tenants.Current.Value;
            using (_tenants.Begin("inside"))
            {
                _insideTenant = _tenants.Current.Value;
            }
            _restoredTenant = _tenants.Current.Value;
            await context.Write(_requestTenant);
        });
        using (_tenants.Begin("tenant-A"))
        {
            StartEndpointMapper();
        }
    }

    async Task Because()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/tenant");
        request.Headers.Add("X-Tenant", "tenant-B");
        using var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        (await response.Content.ReadAsStringAsync()).ShouldEqual("tenant-B");
    }

    [Fact] void should_resolve_the_request_tenant() => _requestTenant.ShouldEqual("tenant-B");
    [Fact] void should_honor_a_scope_opened_inside_the_request() => _insideTenant.ShouldEqual("inside");
    [Fact] void should_restore_the_request_tenant_after_the_inner_scope() => _restoredTenant.ShouldEqual("tenant-B");
}
