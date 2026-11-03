// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.given;

public class a_tenant_admission : an_observable_query_demultiplexer
{
    protected IHttpRequestContext _context;
    protected IWebSocketContext _webSockets;
    protected ITenantMembership _membership;
    protected ITenantIdResolver _resolver;
    protected bool _allow;
    protected bool _resolverFails;
    protected bool _hookThrows;
    protected bool _registerMembership = true;
    ServiceProvider _requestServices;

    void Establish()
    {
        _context = Substitute.For<IHttpRequestContext>();
        _webSockets = Substitute.For<IWebSocketContext>();
        _context.WebSockets.Returns(_webSockets);
        _context.RequestAborted.Returns(new CancellationToken(true));
        _httpRequestContextAccessor.Current.Returns(_context);
        _resolver = Substitute.For<ITenantIdResolver>();
        _resolver.Resolve().Returns(_ => _resolverFails ? throw new InvalidOperationException("Unavailable") : "selected");
        _membership = Substitute.For<ITenantMembership>();
        _membership.IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(_ => _hookThrows ? throw new InvalidOperationException("Unavailable") : _allow);
    }

    protected void BuildRequestServices()
    {
        var services = new ServiceCollection();
        if (_registerMembership)
        {
            services.AddSingleton(_membership);
        }
        services.AddSingleton(_resolver);
        services.AddSingleton(new TenantMembershipVerification(_httpRequestContextAccessor, Substitute.For<ICurrentPrincipalAccessor>(), NullLogger<TenantMembershipVerification>.Instance));
        services.AddSingleton(provider => new TenantIdAccessor(_resolver, provider.GetRequiredService<TenantMembershipVerification>()));
        _requestServices = services.BuildServiceProvider();
        _context.RequestServices.Returns(_requestServices);
    }

    void Destroy() => _requestServices?.Dispose();
}
