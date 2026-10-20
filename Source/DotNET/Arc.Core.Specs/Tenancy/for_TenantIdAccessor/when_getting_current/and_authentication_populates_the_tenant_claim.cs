// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_authentication_populates_the_tenant_claim : Specification
{
    const string ExpectedTenantId = "test-tenant";
    TenantIdAccessor _accessor;
    IHttpRequestContext _context;
    TenantId _beforeAuthentication;
    TenantId _afterAuthentication;
    TenantId _afterClaimChanges;

    void Establish()
    {
        _context = Substitute.For<IHttpRequestContext>();
        _context.User.Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        requests.Current.Returns(_context);
        var resolver = new ClaimTenantIdResolver(requests, Options.Create(new ArcOptions()));
        _accessor = new TenantIdAccessor(resolver);
        _beforeAuthentication = _accessor.Current;
    }

    void Because()
    {
        _context.User.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", ExpectedTenantId)], "test")));
        _afterAuthentication = _accessor.Current;
        _context.User.Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", "other-tenant")], "test")));
        _afterClaimChanges = _accessor.Current;
    }

    [Fact] void should_return_not_set_before_authentication() => _beforeAuthentication.ShouldEqual(TenantId.NotSet);
    [Fact] void should_resolve_the_tenant_after_authentication() => _afterAuthentication.ShouldEqual(new TenantId(ExpectedTenantId));
    [Fact] void should_keep_the_resolved_tenant_cached() => _afterClaimChanges.ShouldEqual(_afterAuthentication);
}
