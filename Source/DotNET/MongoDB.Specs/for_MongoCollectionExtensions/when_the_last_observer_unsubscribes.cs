// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions;

public class when_the_last_observer_unsubscribes : given.a_controlled_watch
{
    [Theory]
    [InlineData("single")]
    [InlineData("by_id")]
    [InlineData("list")]
    public async Task should_leave_the_shared_stream_only_after_the_last_subscription_is_disposed(string kind)
    {
        await StartObserving(kind);
        using var secondSubscription = _observable.Subscribe(_ => { });
        _databaseChanges.Subscribers.ShouldEqual(1);

        _subscription.Dispose();
        _databaseChanges.Subscribers.ShouldEqual(1);

        secondSubscription.Dispose();
        await WaitUntil(() => _databaseChanges.Subscribers == 0);
        _databaseChanges.Subscribers.ShouldEqual(0);
        Catch.Exception(secondSubscription.Dispose).ShouldBeNull();
    }

    static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + Deadline;
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10);
        }
    }
}
