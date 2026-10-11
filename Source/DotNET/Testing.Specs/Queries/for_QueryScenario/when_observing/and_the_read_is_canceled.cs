// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_read_is_canceled : given.an_observable_query
{
    Exception? _exception;
    QueryResult _result = default!;

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Current));
        using var cancellation = new CancellationTokenSource();
        var next = observation.Next(cancellationToken: cancellation.Token);
        await cancellation.CancelAsync();
        _exception = await Catch.Exception(() => next);
        _state.Subject.OnNext(new("Still subscribed"));
        _result = await observation.Next();
    }

    [Fact] void should_report_cancellation() => (_exception is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_not_report_a_timeout() => (_exception is TimeoutException).ShouldBeFalse();
    [Fact] void should_keep_the_subscription_alive() => ((ObservableScenarioReadModel)_result.Data).Name.ShouldEqual("Still subscribed");
}
