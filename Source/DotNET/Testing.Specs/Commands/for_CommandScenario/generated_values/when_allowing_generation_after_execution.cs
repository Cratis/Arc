// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_allowing_generation_after_execution : given.a_generated_value_scenario
{
    Exception _error;

    async Task Establish() => await _scenario.Execute(new given.AllocateCartId());
    void Because() => _error = Catch.Exception(() => _scenario.AllowGeneration());

    [Fact] void should_refuse_to_change_strictness() => _error.ShouldBeOfExactType<GeneratedValuesMustBePinnedBeforeExecution>();
}
