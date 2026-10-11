// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_next_times_out_during_a_guard : given.a_blocked_emission_guard
{
    Exception? _exception;
    QueryResult _retried = default!;

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Current));
        _state.Subject.OnNext(new("Held"));
        var next = observation.Next(TimeSpan.FromMilliseconds(50));
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _exception = await Catch.Exception(() => next);
        _release.TrySetResult();
        _retried = await observation.Next();
    }

    [Fact] void should_time_out_the_read() => _exception.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_preserve_the_in_flight_emission() => ((ObservableScenarioReadModel)_retried.Data).Name.ShouldEqual("Held");
    [Fact] void should_succeed_after_the_timeout() => _retried.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_repeat_the_guard_for_the_held_emission() => _guards.Received(2).Guard(Arg.Any<ObservableQueryEmissionContext>());
}
