// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_guarding_an_emission;

public class and_the_guard_suppresses : given.a_request
{
    void Establish() => _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(Task.FromResult(ObservableQueryEmissionVerdict.Suppress));

    async Task Because()
    {
        using var subject = new BehaviorSubject<string>("withheld");
        _response = await ObservableQueryHttp.CreateResponse(_queryContext, subject, new(false, TimeSpan.FromSeconds(1)), _request);
    }

    [Fact] void should_return_accepted() => _response.StatusCode.ShouldEqual(HttpStatusCode.Accepted);
    [Fact] void should_not_include_data() => _response.Result.Data.ShouldBeNull();
    [Fact] void should_report_not_ready() => _response.Result.IsReady.ShouldBeFalse();
}
