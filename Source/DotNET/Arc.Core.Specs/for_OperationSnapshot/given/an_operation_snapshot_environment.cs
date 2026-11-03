// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.for_OperationSnapshot.given;

public class an_operation_snapshot_environment : Specification
{
    protected ITenantIdResolver _resolver;
    protected TenantIdAccessor _accessor;
    protected ICurrentPrincipalAccessor _principals;
    protected IServiceProvider _services;
    protected ClaimsPrincipal _user;
    IDisposable _boundary;

    void Establish()
    {
        _boundary = TenantIdAccessor.Independent.BeginRequest();
        _resolver = Substitute.For<ITenantIdResolver>();
        _resolver.Resolve().Returns("tenant-1");
        _accessor = new TenantIdAccessor(_resolver);
        _user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1"), new Claim("name", "User")], "test"));
        _principals = Substitute.For<ICurrentPrincipalAccessor>();
        _principals.Current.Returns(_user);
        _services = Substitute.For<IServiceProvider>();
        _services.GetService(typeof(ICurrentPrincipalAccessor)).Returns(_principals);
        _services.GetService(typeof(TenantIdAccessor)).Returns(_accessor);
        _services.GetService(typeof(ITenantIdAccessor)).Returns(_accessor);
        _services.GetService(typeof(ITenantIdResolver)).Returns(_resolver);
    }

    void Destroy() => _boundary.Dispose();
}
