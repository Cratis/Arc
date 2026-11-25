// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Delegation;
using Microsoft.Extensions.DependencyInjection;
using VerifiedCallerConsumer;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerConsumer.given;

public class a_delegated_consumer : Specification
{
    protected ServiceCollection _services;
    protected IVerifiedCallerAccessor _accessor;
    protected IDelegationAuthority _authority;
    protected VerifiedCaller _caller;
    protected VerifiedCaller? _operationCaller;
    protected string? _result;
    protected bool _productAllows = true;
    ServiceProvider? _provider;
    IServiceScope? _scope;

    void Establish()
    {
        _services = new();
        _accessor = Substitute.For<IVerifiedCallerAccessor>();
        _authority = Substitute.For<IDelegationAuthority, IDisposable>();
        _authority.IsAllowed(Arg.Any<DelegationRequest>(), Arg.Any<CancellationToken>()).Returns(true);
        _caller = new VerifiedCallerBuilder(Callers.given.verified_facts.Catalog).Build(Callers.given.verified_facts.Delegated(), Callers.given.verified_facts.Installation, Callers.given.verified_facts.Tenant).Caller!;
        _accessor.GetCurrent().Returns(new CallerResolution(_caller, null));
        _services.AddScoped(_ => _accessor);
        _services.AddScoped<DelegationBoundary>();
        _services.AddScoped<DelegatedToolHandler>();
    }

    protected async Task InvokeConsumer()
    {
        _provider = _services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        _scope = _provider.CreateScope();
        var handler = _scope.ServiceProvider.GetRequiredService<DelegatedToolHandler>();
        _result = await handler.TryRead(caller =>
        {
            if (!_productAllows) return "product-refused";
            _operationCaller = caller;
            return "product-authorized";
        });
    }

    protected IServiceProvider CurrentScope => _scope!.ServiceProvider;

    protected void DisposeScope() => _scope!.Dispose();

    void Destroy()
    {
        _scope?.Dispose();
        _provider?.Dispose();
    }
}
