// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_intercepting_an_emission;

public class and_the_interceptor_fails : given.a_request
{
    void Establish() => _interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
        .Returns(Task.FromException<IEnumerable<object>>(new InterceptionFailed()));

    async Task Because()
    {
        using var subject = new BehaviorSubject<string>("raw");
        _response = await ObservableQueryHttp.CreateResponse(_queryContext, subject, new(false, TimeSpan.FromSeconds(1)), _request);
    }

    [Fact] void should_return_an_error() => _response.StatusCode.ShouldEqual(HttpStatusCode.InternalServerError);
    [Fact] void should_not_return_raw_data() => _response.Result.Data.ShouldBeNull();
    [Fact] void should_report_failure() => _response.Result.IsSuccess.ShouldBeFalse();

    class InterceptionFailed : Exception;
}
