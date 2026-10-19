// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.DependencyInjection.for_ProtectedDecisionServiceProviderFactory.when_tracking_native_scopes.given;

public class two_roots : Specification
{
    protected ServiceCollection _services;
    protected ProtectedDecisionServiceProviderFactory _factory;
    protected ServiceProvider _first;
    protected ServiceProvider _second;
    protected IServiceScope _scope;

    void Establish()
    {
        _services = new();
        _factory = new(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        _first = (ServiceProvider)_factory.CreateServiceProvider(_factory.CreateBuilder(_services));
        _second = (ServiceProvider)_factory.CreateServiceProvider(_factory.CreateBuilder(_services));
        _scope = _first.CreateScope();
    }

    void Destroy()
    {
        _scope.Dispose();
        _second.Dispose();
        _first.Dispose();
    }
}
