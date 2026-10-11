// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_pinning_after_a_command_without_generated_values : Specification
{
    CommandScenario<FinishWork> _scenario;
    Exception _error;

    async Task Establish()
    {
        _scenario = new();
        (await _scenario.Execute(new FinishWork())).ShouldBeSuccessful();
    }

    void Because() => _error = Catch.Exception(() => _scenario.PinGeneratedValue<FinishWork, given.AllocateCartId>("cart", given.CartId.New()));
    void Destroy() => _scenario.Dispose();

    [Fact] void should_freeze_all_pins_at_the_first_execution() => _error.ShouldBeOfExactType<GeneratedValuesMustBePinnedBeforeExecution>();

    [Command]
    public record FinishWork
    {
        public string Handle() => "Finished";
    }
}
