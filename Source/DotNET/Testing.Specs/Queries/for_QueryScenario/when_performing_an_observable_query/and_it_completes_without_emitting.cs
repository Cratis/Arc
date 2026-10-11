// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_performing_an_observable_query;

public class and_it_completes_without_emitting : given.an_observable_query
{
    QueryResult _result = default!;

    void Establish()
    {
        _state.Subject = new Subject<ObservableScenarioReadModel>();
        _state.Subject.OnCompleted();
    }

    async Task Because() => _result = await _scenario.Perform(nameof(ObservableScenarioReadModel.Current));

    [Fact] void should_report_the_http_completion_message() => _result.ExceptionMessages.ShouldContainOnly("Observable query completed before producing its first result.");
    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
}
