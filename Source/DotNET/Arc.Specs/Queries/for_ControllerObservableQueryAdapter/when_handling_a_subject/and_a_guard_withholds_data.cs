// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ControllerObservableQueryAdapter.when_handling_a_subject;

public class and_a_guard_withholds_data : given.a_request_with_emission_processing
{
    [Theory]
    [InlineData(ObservableQueryEmissionVerdict.Suppress, 202)]
    [InlineData(ObservableQueryEmissionVerdict.DenyAndTerminate, 403)]
    async Task should_preserve_the_http_status_and_withhold_the_payload(ObservableQueryEmissionVerdict verdict, int status)
    {
        _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(Task.FromResult(verdict));
        using var subject = new BehaviorSubject<TestData>(new("private"));
        await Handle(subject);
        _result.StatusCode.ShouldEqual(status);
        ((QueryResult)_result.Value!).Data.ShouldBeNull();
    }
}
