// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Arc.Queries.for_ObservableQueryHandler.when_handling_streaming_result;

public class and_a_value_type_async_enumerable_is_requested_over_sse : given.a_handler_over_a_real_container
{
    Exception _error;

    void Establish() => ConnectOverSse();

    async Task Because() => _error = await Catch.Exception(() => _handler.HandleStreamingResult(_context, StreamingQueryName, Numbers()));

    [Fact] void should_construct_the_observable() => _error.ShouldBeNull();
    [Fact] void should_write_every_item_to_the_client() => _written.Count.ShouldEqual(3);

    static async IAsyncEnumerable<int> Numbers([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in new[] { 1, 2, 3 })
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }
}
