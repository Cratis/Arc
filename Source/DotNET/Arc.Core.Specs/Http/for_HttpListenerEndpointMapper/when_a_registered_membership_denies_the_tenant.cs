// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Claims;
using Cratis.Arc.Authentication;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper;

public class when_a_registered_membership_denies_the_tenant : given.a_running_endpoint_mapper
{
    ServiceProvider _services;
    HttpResponseMessage _response;
    string _body;

    void Establish()
    {
        var requests = new HttpRequestContextAccessor();
        var resolver = Substitute.For<ITenantIdResolver>();
        resolver.Resolve().Returns("selected");
        var membership = Substitute.For<ITenantMembership>();
        membership.IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(false);
        var verification = new TenantMembershipVerification(requests, new CurrentPrincipalAccessor(requests), NullLogger<TenantMembershipVerification>.Instance);
        _services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHttpRequestContextAccessor>(requests)
            .AddSingleton(Substitute.For<IAuthentication>())
            .AddSingleton(membership)
            .AddSingleton(new TenantIdAccessor(resolver, verification))
            .BuildServiceProvider();
        ((IDisposable)_serviceProvider).Dispose();
        _serviceProvider = _services;
        _endpointMapper.MapGet("/denied", context =>
        {
            _ = context.RequestServices.GetRequiredService<TenantIdAccessor>().Current;
            return Task.CompletedTask;
        });
        StartEndpointMapper();
    }

    async Task Because()
    {
        _response = await _httpClient.GetAsync("/denied", TestContext.Current.CancellationToken);
        _body = await _response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    void Destroy()
    {
        _response?.Dispose();
        _services?.Dispose();
    }

    [Fact] void should_respond_forbidden() => _response.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_not_return_details() => _body.ShouldEqual(string.Empty);
}
