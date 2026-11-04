// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Commands.for_CommandPipeline.when_executing;

public class and_it_is_traced_for_an_explicit_tenant : given.a_traced_command_pipeline
{
    TenantIdAccessor _tenants;
    TenantId _tenant;

    void Establish()
    {
        _tenant = new("acme");
        _tenants = new(Substitute.For<ITenantIdResolver>());
        _serviceProvider.GetService(typeof(TenantIdAccessor)).Returns(_tenants);
    }

    async Task Because()
    {
        using var tenantScope = _tenants.Begin(_tenant);
        _result = await _commandPipeline.Execute(_command, _serviceProvider);
    }

    [Fact] void should_add_the_tenant() => CommandSpan.GetTagItem(WellKnownTelemetryNames.Tenant).ShouldEqual(_tenant.Value);
    [Fact] void should_add_the_aligned_tenant() => CommandSpan.GetTagItem(WellKnownTelemetryNames.TenantId).ShouldEqual(_tenant.Value);
    [Fact] void should_record_duration_inside_the_operation_span() => Durations.Single().ActivityId.ShouldEqual(CommandSpan.Id);
}
