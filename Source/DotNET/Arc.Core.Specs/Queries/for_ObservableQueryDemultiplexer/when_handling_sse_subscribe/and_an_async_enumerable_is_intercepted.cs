// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_an_async_enumerable_is_intercepted : given.a_guarded_sse_connection
{
    IServiceProvider _interceptionProvider;

    void Establish()
    {
        _streamingData = OneBatch();
        _readModelInterceptors.Intercept(typeof(string), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>()).Returns(call =>
        {
            _interceptionProvider = call.Arg<IServiceProvider>();
            return Task.FromResult<IEnumerable<object>>(["intercepted"]);
        });
    }

    static async IAsyncEnumerable<IEnumerable<string>> OneBatch()
    {
        await Task.Yield();
        yield return ["raw"];
    }

    async Task Because() => await RunConnection(() => WaitFor(() => CountQueryResultsFor(FirstQueryId) == 1));

    [Fact] void should_deliver_intercepted_data() => ((JsonElement)QueryResultsFor(FirstQueryId).Single().Data)[0].GetString().ShouldEqual("intercepted");
    [Fact] void should_intercept_in_the_same_scope_as_the_guard() => _interceptionProvider.ShouldEqual(_guardCalls.Single().ServiceProvider);
    [Fact] void should_not_intercept_from_the_root_provider() => (_interceptionProvider == _guardedServiceProvider).ShouldBeFalse();
}
