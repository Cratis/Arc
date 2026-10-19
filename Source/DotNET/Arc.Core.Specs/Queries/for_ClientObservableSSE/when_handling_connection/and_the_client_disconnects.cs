// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservableSSE.when_handling_connection;

public class and_the_client_disconnects : given.a_telemetered_client_observable_sse
{
    readonly CancellationTokenSource _aborted = new();

    void Establish() => _context.RequestAborted.Returns(_aborted.Token);

    void Destroy() => _aborted.Dispose();

    async Task Because() => await RunConnection(async () =>
    {
        await _aborted.CancelAsync();
        await Task.Delay(50);
    });

    [Fact] void should_count_the_subscription_up_and_then_down() => _observed.Subscriptions.Select(_ => _.Value).ShouldContainOnly(1, -1);
    [Fact] void should_leave_no_subscription_active() => _observed.ActiveSubscriptions.ShouldEqual(0);
}
