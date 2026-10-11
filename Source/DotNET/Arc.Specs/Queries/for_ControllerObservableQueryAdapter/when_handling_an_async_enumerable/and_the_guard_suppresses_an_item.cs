// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ControllerObservableQueryAdapter.when_handling_an_async_enumerable;

public class and_the_guard_suppresses_an_item : given.a_request_with_emission_processing
{
    void Establish() => _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(
        Task.FromResult(ObservableQueryEmissionVerdict.Suppress),
        Task.FromResult(ObservableQueryEmissionVerdict.Allow));

    Task Because() => Handle(TwoItems());

    [Fact] void should_return_only_the_allowed_item() => ((IEnumerable<object>)_result.Value!).Count().ShouldEqual(1);
    [Fact] async Task should_keep_the_next_item_as_the_first_emission() => await _guards.Received(2).Guard(Arg.Is<ObservableQueryEmissionContext>(context => context.IsFirstEmission));
}
