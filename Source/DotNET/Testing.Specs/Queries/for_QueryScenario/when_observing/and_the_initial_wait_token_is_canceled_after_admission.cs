// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_initial_wait_token_is_canceled_after_admission : given.an_observable_query
{
    QueryResult _result = default!;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Current), cancellationToken: cancellation.Token);
        await cancellation.CancelAsync();
        _state.Subject.OnNext(new("Still live"));
        _result = await observation.Next();
    }

    [Fact] void should_not_cancel_the_query_context() => _state.Context.CancellationToken.IsCancellationRequested.ShouldBeFalse();
    [Fact] void should_not_attach_the_wait_token_to_the_query_context() => _state.Context.CancellationToken.CanBeCanceled.ShouldBeFalse();
    [Fact] void should_continue_receiving_emissions() => ((ObservableScenarioReadModel)_result.Data).Name.ShouldEqual("Still live");
}
