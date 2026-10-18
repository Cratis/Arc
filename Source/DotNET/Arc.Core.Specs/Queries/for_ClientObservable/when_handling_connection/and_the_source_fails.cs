// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class and_the_source_fails : given.a_telemetered_client_observable
{
    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnError(new InvalidOperationException("The source failed"));
        await WaitFor(() => ServerEndedTheConnection);
    });

    [Fact] void should_count_the_subscription_up_and_then_down() => _observed.Subscriptions.Select(_ => _.Value).ShouldContainOnly(1, -1);
    [Fact] void should_leave_no_subscription_active() => _observed.ActiveSubscriptions.ShouldEqual(0);
}
