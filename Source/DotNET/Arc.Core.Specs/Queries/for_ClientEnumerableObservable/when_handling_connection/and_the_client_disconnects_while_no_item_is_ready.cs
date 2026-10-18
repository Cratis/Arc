// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.when_handling_connection;

public class and_the_client_disconnects_while_no_item_is_ready : given.a_telemetered_client_enumerable_observable
{
    Exception? _error;

    protected override IAsyncEnumerable<string> Items() => NoItemsYet();

    async Task Because()
    {
        var connection = _observable.HandleConnection(_context);
        await Task.Delay(50);
        _incomingCompleted.TrySetResult();
        _error = await Catch.Exception(async () => await connection.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact] void should_end_the_connection() => _error.ShouldBeNull();
    [Fact] void should_leave_no_subscription_active() => _observed.ActiveSubscriptions.ShouldEqual(0);
    [Fact] void should_count_no_push() => _observed.Pushes.ShouldBeEmpty();

    static async IAsyncEnumerable<string> NoItemsYet([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        yield break;
    }
}
