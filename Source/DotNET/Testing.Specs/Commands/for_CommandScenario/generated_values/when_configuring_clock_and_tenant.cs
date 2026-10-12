// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_configuring_clock_and_tenant : Specification
{
    readonly DateTimeOffset _instant = new(2024, 6, 15, 12, 34, 56, TimeSpan.Zero);
    readonly TenantId _tenant = new("tenant-one");
    CommandScenario<given.ObserveGeneration> _scenario;
    given.GenerationObservation _observation;

    void Establish() => _scenario = new CommandScenario<given.ObserveGeneration>().Clock(_instant).Tenant(_tenant).AllowGeneration();
    async Task Because() => _observation = ((CommandResult<given.GenerationObservation>)await _scenario.Execute(new())).Response!;
    void Destroy() => _scenario.Dispose();

    [Fact] void should_generate_with_the_fixed_context() => _observation.Id.Value.ShouldEqual($"{_instant:O}|{_tenant.Value}");
    [Fact] void should_set_the_command_received_time() => _observation.ReceivedAt.ShouldEqual(_instant);
    [Fact] void should_replace_the_scenario_clock_for_other_consumers() => _observation.Clock.ShouldEqual(_instant);
    [Fact] void should_fix_local_wall_clock_reads_too() => _observation.LocalClock.ToUniversalTime().ShouldEqual(_instant);
    [Fact] void should_select_the_scenario_tenant() => _observation.Tenant.ShouldEqual(_tenant);
    [Fact] void should_capture_the_generated_value() => _scenario.GeneratedValues.Last<given.ContextId>("id").ShouldEqual(_observation.Id);
    [Fact] void should_refuse_clock_changes_after_execution() => Catch.Exception(() => _scenario.Clock(_instant.AddDays(1))).ShouldBeOfExactType<GeneratedValuesMustBePinnedBeforeExecution>();
    [Fact] void should_refuse_tenant_changes_after_execution() => Catch.Exception(() => _scenario.Tenant(TenantId.Default)).ShouldBeOfExactType<GeneratedValuesMustBePinnedBeforeExecution>();
}
