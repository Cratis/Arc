// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_intercepting_an_emission;

public class and_the_cached_subject_completed : given.a_request
{
    async Task Because()
    {
        using var subject = new BehaviorSubject<string>("completed");
        subject.OnCompleted();
        _response = await ObservableQueryHttp.CreateResponse(_queryContext, subject, new(true, TimeSpan.FromSeconds(1)), _request);
    }

    [Fact]
    void should_intercept_the_cache_even_if_the_subject_completed()
    {
        _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
        _response.Result.Data.ShouldEqual("completed-intercepted");
    }
}
