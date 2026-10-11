// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ControllerObservableQueryAdapter.when_handling_an_async_enumerable;

public class and_http_is_requested : given.a_request_with_emission_processing
{
    Task Because() => Handle(TwoItems());

    [Fact] void should_preserve_the_raw_collection_success_shape() => ((IEnumerable<object>)_result.Value!).Count().ShouldEqual(2);
    [Fact] void should_intercept_every_item() => ((IEnumerable<object>)_result.Value!).Cast<TestData>().All(item => item.Value == "intercepted").ShouldBeTrue();
    [Fact] void should_return_ok() => _result.StatusCode.ShouldEqual(200);
    [Fact] async Task should_guard_every_item() => await _guards.Received(2).Guard(Arg.Any<ObservableQueryEmissionContext>());
}
