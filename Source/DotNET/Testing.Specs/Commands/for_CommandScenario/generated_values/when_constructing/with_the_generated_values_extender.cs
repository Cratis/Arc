// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values.when_constructing;

public class with_the_generated_values_extender : Specification
{
    CommandScenario<given.AllocateCartId> _scenario;

    void Because() => _scenario = new();
    void Destroy() => _scenario.Dispose();

    [Fact] void should_discover_the_extender_without_registration() => _scenario.Context.ContainsKey(GeneratedValuesCommandScenarioExtender.ContextKey).ShouldBeTrue();
    [Fact] void should_start_with_no_captures() => _scenario.GeneratedValues.All<given.CartId>("cart").ShouldBeEmpty();
    [Fact] void should_register_the_scenario_local_fixture() => _scenario.Services.Single(service => service.ServiceType == typeof(IGeneratedValueFixture)).ImplementationInstance.ShouldBeOfExactType<ScenarioGeneratedValueFixture>();
}
