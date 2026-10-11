// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_performing_an_observable_query;

public class and_it_has_a_current_value : given.an_observable_query
{
    QueryResult _result = default!;

    async Task Because() => _result = await _scenario.Perform(nameof(ObservableScenarioReadModel.Current));

    [Fact] void should_return_the_current_value() => ((ObservableScenarioReadModel)_result.Data).Name.ShouldEqual("Initial");
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_release_the_subscription() => ((BehaviorSubject<ObservableScenarioReadModel>)_state.Subject).HasObservers.ShouldBeFalse();
    [Fact] void should_leave_the_subject_usable() => _state.Subject.OnNext(new("Still usable"));
}
