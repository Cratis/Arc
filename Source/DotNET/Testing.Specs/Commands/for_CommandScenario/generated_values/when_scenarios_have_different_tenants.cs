// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_scenarios_have_different_tenants : Specification
{
    CommandScenario<given.ObserveGeneration> _first;
    CommandScenario<given.ObserveGeneration> _second;
    given.GenerationObservation _one;
    given.GenerationObservation _two;
    TenantId _after;

    void Establish()
    {
        _first = new CommandScenario<given.ObserveGeneration>().Tenant(new TenantId("one")).AllowGeneration();
        _second = new CommandScenario<given.ObserveGeneration>().Tenant(new TenantId("two")).AllowGeneration();
    }

    async Task Because()
    {
        var accessor = new TenantIdAccessor(Substitute.For<ITenantIdResolver>());
        using var tenant = accessor.Begin(new TenantId("caller"));
        _one = ((CommandResult<given.GenerationObservation>)await _first.Execute(new())).Response!;
        _two = ((CommandResult<given.GenerationObservation>)await _second.Execute(new())).Response!;
        _after = accessor.Current;
    }

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
    }

    [Fact] void should_select_the_first_scenarios_tenant() => _one.Tenant.ShouldEqual(new TenantId("one"));
    [Fact] void should_select_the_second_scenarios_tenant() => _two.Tenant.ShouldEqual(new TenantId("two"));
    [Fact] void should_restore_the_callers_tenant() => _after.ShouldEqual(new TenantId("caller"));
}
