// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification.given;

public class a_tenant_membership_verification : Specification
{
    protected IHttpRequestContextAccessor _requests;
    protected IHttpRequestContext _request;
    protected IServiceProvider _services;
    protected ICurrentPrincipalAccessor _principals;
    protected ITenantMembership _membership;
    internal TenantMembershipVerification _verification;
    internal ILogger<TenantMembershipVerification> _logger;
    protected ClaimsPrincipal _principal;
    protected TenantId _tenant;

    void Establish()
    {
        _requests = Substitute.For<IHttpRequestContextAccessor>();
        _request = Substitute.For<IHttpRequestContext>();
        _services = Substitute.For<IServiceProvider>();
        _principals = Substitute.For<ICurrentPrincipalAccessor>();
        _membership = Substitute.For<ITenantMembership>();
        _principal = new ClaimsPrincipal(new ClaimsIdentity("test"));
        _tenant = new TenantId("selected");
        _principals.Current.Returns(_principal);
        _requests.Current.Returns(_request);
        _request.RequestServices.Returns(_services);
        _services.GetService(typeof(IEnumerable<ITenantMembership>)).Returns(new[] { _membership });
        _membership.IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>()).Returns(true);
        _logger = Substitute.For<ILogger<TenantMembershipVerification>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _verification = new TenantMembershipVerification(_requests, _principals, _logger);
    }
}
