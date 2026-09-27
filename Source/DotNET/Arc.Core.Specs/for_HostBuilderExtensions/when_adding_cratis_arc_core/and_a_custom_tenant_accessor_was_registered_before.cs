// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.for_HostBuilderExtensions.when_adding_cratis_arc_core;

public class and_a_custom_tenant_accessor_was_registered_before : Specification
{
    ITenantIdAccessor _custom;
    ITenantIdAccessor _effective;
    Exception _error;

    void Because()
    {
        _custom = Substitute.For<ITenantIdAccessor>();
        var registration = new ServiceCollection();
        registration.AddSingleton(_custom);
        registration.AddCratisArcCore();
        using var services = registration.BuildServiceProvider();
        _effective = services.GetRequiredService<ITenantIdAccessor>();
        _error = Catch.Exception(() => services.GetRequiredService<ITenantScope>().Begin("acme"));
    }

    [Fact] void should_preserve_the_custom_accessor() => ReferenceEquals(_custom, _effective).ShouldBeTrue();
    [Fact] void should_reject_explicit_scopes_instead_of_diverging() => _error.ShouldBeOfExactType<ExplicitTenantScopeRequiresArcAccessor>();
}
