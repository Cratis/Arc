// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Security.Claims;
using Cratis.Arc.Authentication;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http.for_HttpListenerEndpointMapper;

public class when_a_websocket_tenant_membership_is_denied : given.a_running_endpoint_mapper
{
    ServiceProvider _services;
    HttpRequestMessage _request;
    HttpResponseMessage _response;
    bool _isWebSocketRequest;

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
            .AddSingleton(resolver)
            .AddSingleton(membership)
            .AddSingleton(verification)
            .AddSingleton(new TenantIdAccessor(resolver, verification))
            .BuildServiceProvider();
        ((IDisposable)_serviceProvider).Dispose();
        _serviceProvider = _services;
        var hub = new ObservableQueryDemultiplexer(
            Substitute.For<IQueryPipeline>(),
            Substitute.For<IQueryContextManager>(),
            requests,
            Substitute.For<IHostApplicationLifetime>(),
            Substitute.For<IReadModelInterceptors>(),
            _services,
            Options.Create(new ArcOptions()),
            Substitute.For<IQueryHealthTracker>(),
            Substitute.For<IObservableQueryEmissionGuards>(),
            NullLogger<ObservableQueryDemultiplexer>.Instance);
        _endpointMapper.MapGet("/observe", context =>
        {
            _isWebSocketRequest = context.WebSockets.IsWebSocketRequest;
            return hub.HandleWebSocketConnection(context);
        });
        StartEndpointMapper();
        _request = new HttpRequestMessage(HttpMethod.Get, "/observe");
        _request.Headers.Add("Connection", "Upgrade");
        _request.Headers.Add("Upgrade", "websocket");
        _request.Headers.Add("Sec-WebSocket-Version", "13");
        _request.Headers.Add("Sec-WebSocket-Key", Convert.ToBase64String(new byte[16]));
    }

    async Task Because()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        _response = await _httpClient.SendAsync(_request, timeout.Token);
    }

    void Destroy()
    {
        _request?.Dispose();
        _response?.Dispose();
        _services?.Dispose();
    }

    [Fact] void should_exercise_a_websocket_handshake() => _isWebSocketRequest.ShouldBeTrue();
    [Fact] void should_send_a_completed_forbidden_response() => _response.StatusCode.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_not_upgrade() => _response.Headers.Upgrade.ShouldBeEmpty();
}
