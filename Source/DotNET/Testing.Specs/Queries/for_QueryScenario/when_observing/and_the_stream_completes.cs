// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_stream_completes : given.an_observable_query
{
    QueryResult _result = default!;

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Current));
        _state.Subject.OnCompleted();
        _result = await observation.Next();
    }

    [Fact] void should_report_completion() => _result.ExceptionMessages.ShouldContainOnly("Observable query completed before producing its first result.");
    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
}
