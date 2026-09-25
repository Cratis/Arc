// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions;

public class when_the_last_observer_unsubscribes : given.a_controlled_watch
{
    [Theory]
    [InlineData("single")]
    [InlineData("by_id")]
    [InlineData("list")]
    public async Task should_cancel_only_after_the_last_subscription_is_disposed(string kind)
    {
        await StartObserving(kind);
        using var secondSubscription = _observable.Subscribe(_ => { });
        await using var registration = _watchToken.Register(() => _nextBatch.TrySetCanceled(_watchToken));

        _subscription.Dispose();
        _watchToken.IsCancellationRequested.ShouldBeFalse();

        secondSubscription.Dispose();
        _watchToken.IsCancellationRequested.ShouldBeTrue();
        Catch.Exception(secondSubscription.Dispose).ShouldBeNull();
    }
}
