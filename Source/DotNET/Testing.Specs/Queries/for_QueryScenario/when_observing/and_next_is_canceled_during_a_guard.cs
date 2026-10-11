// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_next_is_canceled_during_a_guard : given.a_blocked_emission_guard
{
    Exception? _exception;
    QueryResult _retried = default!;
    QueryResult _subsequent = default!;

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Current));
        using var cancellation = new CancellationTokenSource();
        _state.Subject.OnNext(new("Held"));
        var next = observation.Next(cancellationToken: cancellation.Token);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cancellation.CancelAsync();
        _exception = await Catch.Exception(() => next);
        _release.TrySetResult();
        _retried = await observation.Next();
        _state.Subject.OnNext(new("Subsequent"));
        _subsequent = await observation.Next();
    }

    [Fact] void should_cancel_only_the_read() => (_exception is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_preserve_the_in_flight_emission() => ((ObservableScenarioReadModel)_retried.Data).Name.ShouldEqual("Held");
    [Fact] void should_keep_the_observation_live() => ((ObservableScenarioReadModel)_subsequent.Data).Name.ShouldEqual("Subsequent");
    [Fact] void should_not_repeat_the_guard_for_the_held_emission() => _guards.Received(3).Guard(Arg.Any<ObservableQueryEmissionContext>());
}
