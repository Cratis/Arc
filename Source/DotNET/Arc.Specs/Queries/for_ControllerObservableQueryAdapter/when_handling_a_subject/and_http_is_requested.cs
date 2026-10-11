// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ControllerObservableQueryAdapter.when_handling_a_subject;

public class and_http_is_requested : given.a_request_with_emission_processing
{
    async Task Because()
    {
        using var subject = new BehaviorSubject<TestData>(new("raw"));
        await Handle(subject);
    }

    [Fact] void should_replace_the_mvc_result_with_intercepted_data() => ((TestData)((QueryResult)_result.Value!).Data).Value.ShouldEqual("intercepted");
    [Fact] void should_set_the_success_status() => _result.StatusCode.ShouldEqual(200);
    [Fact] async Task should_guard_the_first_emission() => await _guards.Received(1).Guard(Arg.Is<ObservableQueryEmissionContext>(context => context.IsFirstEmission && context.ServiceProvider == _services));
}
