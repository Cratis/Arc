// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.given;

public class a_verified_tenant_id_accessor : Specification
{
    protected ITenantIdResolver _resolver;
    protected ITenantMembership _membership;
    protected CurrentPrincipalAccessor _principals;
    protected IHttpRequestContextAccessor _requests;
    protected IHttpRequestContext _request;
    protected TenantIdAccessor _accessor;
    protected ClaimsPrincipal _principal;
    IDisposable _boundary;

    void Establish()
    {
        _boundary = TenantIdAccessor.Independent.BeginRequest();
        _resolver = Substitute.For<ITenantIdResolver>();
        _resolver.Resolve().Returns("selected");
        _membership = Substitute.For<ITenantMembership>();
        _membership.IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(true);
        _requests = Substitute.For<IHttpRequestContextAccessor>();
        _request = Substitute.For<IHttpRequestContext>();
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IEnumerable<ITenantMembership>)).Returns(new[] { _membership });
        _request.RequestServices.Returns(services);
        _principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant", "selected")], "test"));
        _request.User.Returns(_principal);
        _requests.Current.Returns(_request);
        _principals = new CurrentPrincipalAccessor(_requests);
        var verification = new TenantMembershipVerification(_requests, _principals, NullLogger<TenantMembershipVerification>.Instance);
        _accessor = new TenantIdAccessor(_resolver, verification);
    }

    void Destroy() => _boundary.Dispose();
}
