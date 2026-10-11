// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Queries.for_ObservableQueryHttp.when_intercepting_an_emission;

public class and_the_emitted_shape_varies : given.a_request
{
    [Fact]
    async Task should_preserve_null()
    {
        using var subject = new BehaviorSubject<string?>(null);
        var response = await ObservableQueryHttp.CreateResponse(_queryContext, subject, new(false, TimeSpan.FromSeconds(1)), _request);
        response.Result.Data.ShouldBeNull();
        await _interceptors.DidNotReceive().Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>());
    }

    [Fact]
    async Task should_preserve_a_value_type()
    {
        using var subject = new BehaviorSubject<int>(42);
        var response = await ObservableQueryHttp.CreateResponse(_queryContext, subject, new(false, TimeSpan.FromSeconds(1)), _request);
        response.Result.Data.ShouldEqual(42);
        await _interceptors.Received(1).Intercept(typeof(int), Arg.Any<IEnumerable<object>>(), _services);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_intercept_each_collection_item_with_the_item_type(bool empty)
    {
        using var subject = new BehaviorSubject<IEnumerable<string>>(empty ? [] : ["first", "second"]);
        var response = await ObservableQueryHttp.CreateResponse(_queryContext, subject, new(false, TimeSpan.FromSeconds(1)), _request);
        string.Join(',', (IEnumerable<object>)response.Result.Data).ShouldEqual(empty ? string.Empty : "first-intercepted,second-intercepted");
        await _interceptors.Received(1).Intercept(typeof(string), Arg.Any<IEnumerable<object>>(), _services);
    }
}
