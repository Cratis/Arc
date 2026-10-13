// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.generated_values;

public class when_configuring_clock_after_validation : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_reject_the_change_without_mutating_the_clock(bool customProvider)
    {
        var instant = new DateTimeOffset(2024, 6, 15, 12, 34, 56, TimeSpan.Zero);
        await using var scenario = new CommandScenario<given.ObserveGeneration>().AllowGeneration();
        if (customProvider)
        {
            scenario.Services.AddSingleton<TimeProvider>(new FixedClock(instant));
        }
        else
        {
            scenario.Clock(instant);
        }

        await scenario.Validate(new());
        var error = Catch.Exception(() => scenario.Clock(instant.AddDays(1)));
        error.ShouldBeOfExactType<ScenarioClockMustBeConfiguredBeforeInitialization>();
        var observation = ((CommandResult<given.GenerationObservation>)await scenario.Execute(new())).Response!;
        observation.Clock.ShouldEqual(instant);
        observation.ReceivedAt.ShouldEqual(instant);
    }

    sealed class FixedClock(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant;
    }
}
