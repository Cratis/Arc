// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_intercepting_an_emission;

public class and_the_value_is_cached : given.a_request
{
    async Task Because()
    {
        using var subject = new BehaviorSubject<string>("cached");
        _response = await ObservableQueryHttp.CreateResponse(_queryContext, subject, new(false, TimeSpan.FromSeconds(1)), _request);
    }

    [Fact]
    async Task should_intercept_the_cache_even_if_the_subject_completed()
    {
        using var subject = new BehaviorSubject<string>("completed");
        subject.OnCompleted();
        var response = await ObservableQueryHttp.CreateResponse(_queryContext, subject, new(true, TimeSpan.FromSeconds(1)), _request);
        response.StatusCode.ShouldEqual(HttpStatusCode.OK);
        response.Result.Data.ShouldEqual("completed-intercepted");
    }

    [Fact] void should_return_intercepted_data() => _response.Result.Data.ShouldEqual("cached-intercepted");
    [Fact] void should_return_ok() => _response.StatusCode.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_guard_the_first_emission() => _guardContexts.Single().IsFirstEmission.ShouldBeTrue();
    [Fact] void should_use_the_request_provider() => _guardContexts.Single().ServiceProvider.ShouldEqual(_services);
    [Fact] void should_use_the_request_principal() => _guardContexts.Single().Principal.ShouldEqual(_request.User);
}
