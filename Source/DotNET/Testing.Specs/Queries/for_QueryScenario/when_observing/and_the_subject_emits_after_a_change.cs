// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_subject_emits_after_a_change : given.an_observable_query
{
    QueryObservation _observation = default!;
    QueryResult _result = default!;

    async Task Because()
    {
        _observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Current));
        var next = _observation.Next();
        _state.Subject.OnNext(new("Changed"));
        _result = await next;
    }

    [Fact] void should_capture_the_initial_value() => ((ObservableScenarioReadModel)_observation.Initial.Data).Name.ShouldEqual("Initial");
    [Fact] void should_receive_the_change() => ((ObservableScenarioReadModel)_result.Data).Name.ShouldEqual("Changed");
    [Fact] void should_preserve_correlation() => _result.CorrelationId.ShouldEqual(_observation.Initial.CorrelationId);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
}
