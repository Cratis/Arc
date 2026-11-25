// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Callers;
using Cratis.Arc.Identity.Callers.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Identity.Delegation.for_DelegationBoundary.given;

public class a_boundary : Specification
{
    protected ServiceCollection _services;
    protected IDelegationAuthority _authority;
    protected VerifiedCaller _caller;
    protected DelegationRequest _request;
    protected bool _result;
    ServiceProvider? _provider;
    IServiceScope? _scope;

    void Establish()
    {
        _services = new();
        _authority = Substitute.For<IDelegationAuthority>();
        _authority.IsAllowed(Arg.Any<DelegationRequest>(), Arg.Any<CancellationToken>()).Returns(true);
        _caller = new VerifiedCallerBuilder(verified_facts.Catalog).Build(verified_facts.Delegated(), verified_facts.Installation, verified_facts.Tenant).Caller!;
        _request = new(_caller, "fixture:read", _caller.Resource, _caller.Environment);
    }

    protected DelegationBoundary CreateBoundary()
    {
        _provider = _services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        return new(_scope.ServiceProvider);
    }

    void Destroy()
    {
        _scope?.Dispose();
        _provider?.Dispose();
    }
}
