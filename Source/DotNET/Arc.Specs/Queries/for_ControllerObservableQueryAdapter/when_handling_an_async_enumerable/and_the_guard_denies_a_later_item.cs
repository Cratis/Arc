// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ControllerObservableQueryAdapter.when_handling_an_async_enumerable;

public class and_the_guard_denies_a_later_item : given.a_request_with_emission_processing
{
    void Establish() => _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(
        Task.FromResult(ObservableQueryEmissionVerdict.Allow),
        Task.FromResult(ObservableQueryEmissionVerdict.DenyAndTerminate));

    Task Because() => Handle(TwoItems());

    [Fact] void should_return_forbidden() => _result.StatusCode.ShouldEqual(403);
    [Fact] void should_not_return_any_buffered_data() => ((QueryResult)_result.Value!).Data.ShouldBeNull();
}
