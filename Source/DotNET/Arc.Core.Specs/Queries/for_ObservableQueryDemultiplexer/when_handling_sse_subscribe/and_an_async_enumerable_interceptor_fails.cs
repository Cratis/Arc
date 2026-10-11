// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_an_async_enumerable_interceptor_fails : given.a_guarded_sse_connection
{
    void Establish()
    {
        _streamingData = OneBatch();
        _readModelInterceptors.Intercept(typeof(string), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>())
            .Returns(Task.FromException<IEnumerable<object>>(new InterceptionFailed()));
    }

    static async IAsyncEnumerable<IEnumerable<string>> OneBatch()
    {
        await Task.Yield();
        yield return ["private"];
    }

    async Task Because() => await RunConnection(() => WaitFor(() => HasErrorFor(FirstQueryId)));

    [Fact] void should_report_a_stream_error() => HasErrorFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_not_deliver_raw_data() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);

    class InterceptionFailed : Exception;
}
