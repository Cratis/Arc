// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_intercepting_an_emission;

public class and_the_value_arrives_later : given.a_request
{
    IHttpRequestContext _interceptionRequest;

    void Establish()
    {
        _interceptors.Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(call =>
            {
                _interceptionRequest = _accessor.Current!;
                return Task.FromResult<IEnumerable<object>>(["delayed-intercepted"]);
            });
    }

    async Task Because()
    {
        using var subject = new Subject<string>();
        var response = ObservableQueryHttp.CreateResponse(_queryContext, subject, new(true, TimeSpan.FromSeconds(2)), _request);

        // The synchronous part of CreateResponse establishes the subscription before yielding to this producer.
        subject.HasObservers.ShouldBeTrue();
        using (ExecutionContext.SuppressFlow())
        {
            _ = Task.Run(() =>
            {
                _accessor.Current = Substitute.For<IHttpRequestContext>();
                subject.OnNext("delayed");
            });
        }
        _response = await response.WaitAsync(TimeSpan.FromSeconds(3));
    }

    [Fact] void should_intercept_the_delayed_value() => _response.Result.Data.ShouldEqual("delayed-intercepted");
    [Fact] void should_restore_the_subscriber_request() => _interceptionRequest.ShouldEqual(_request);
    [Fact] void should_guard_the_first_emission() => _guardContexts.Single().IsFirstEmission.ShouldBeTrue();
}
