// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryHandler;

public class when_checking_if_async_enumerable_is_streaming_result : given.an_observable_query_handler
{
    TestAsyncEnumerable _enumerable;
    bool _result;

    void Establish()
    {
        _enumerable = new TestAsyncEnumerable();
    }

    void Because() => _result = _handler.IsStreamingResult(_enumerable);

    [Fact] void should_return_true() => _result.ShouldBeTrue();

    class TestAsyncEnumerable : IAsyncEnumerable<string>
    {
        public async IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return "test";
        }
    }
}
