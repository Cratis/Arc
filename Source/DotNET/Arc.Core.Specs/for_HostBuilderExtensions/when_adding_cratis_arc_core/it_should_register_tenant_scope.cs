// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

public class it_should_register_tenant_scope : Specification
{
    ITenantScope _scope;
    ITenantIdAccessor _accessor;
    TenantIdAccessor _concrete;

    void Because()
    {
        var registration = new ServiceCollection();
        registration.AddOptions();
        registration.AddCratisArcCore();
        using var services = registration.BuildServiceProvider();
        _scope = services.GetRequiredService<ITenantScope>();
        _accessor = services.GetRequiredService<ITenantIdAccessor>();
        _concrete = services.GetRequiredService<TenantIdAccessor>();
    }

    [Fact] void should_resolve_the_tenant_scope() => _scope.ShouldNotBeNull();
    [Fact] void should_share_the_accessors_instance() => ReferenceEquals(_scope, _accessor).ShouldBeTrue();
    [Fact] void should_share_the_authorization_accessors_instance() => ReferenceEquals(_scope, _concrete).ShouldBeTrue();
}
