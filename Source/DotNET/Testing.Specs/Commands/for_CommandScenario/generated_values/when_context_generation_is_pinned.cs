// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_context_generation_is_pinned : Specification
{
    CommandScenario<given.ObserveGeneration> _scenario;
    readonly given.ContextId _pin = new("known-id");
    given.GenerationObservation _observation;

    void Establish() => _scenario = new CommandScenario<given.ObserveGeneration>().Clock(default).PinGeneratedValue("id", _pin);
    async Task Because() => _observation = ((CommandResult<given.GenerationObservation>)await _scenario.Execute(new())).Response!;
    void Destroy() => _scenario.Dispose();

    [Fact] void should_use_the_pin_without_invoking_the_context_factory() => _observation.Id.ShouldEqual(_pin);
    [Fact] void should_keep_the_configured_clock_for_other_consumers() => _observation.ReceivedAt.ShouldEqual(default);
}
