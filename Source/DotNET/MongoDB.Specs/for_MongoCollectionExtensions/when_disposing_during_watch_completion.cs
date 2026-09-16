// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions;

public class when_disposing_during_watch_completion : given.a_controlled_watch
{
    [Theory]
    [InlineData("single")]
    [InlineData("by_id")]
    [InlineData("list")]
    public async Task should_keep_cancellation_resources_alive_until_the_cancellation_callback_returns(string kind)
    {
        await StartObserving(kind);
        Exception callbackError = null;
        var callbackExecuted = false;
        await using var registration = _watchToken.Register(() =>
        {
            callbackExecuted = true;

            // Cancellation lets the driver finish its pending move. Hold its cancellation callback open
            // until the watch delivers completion, forcing cleanup to overlap Cancel rather than racing it.
            _nextBatch.SetResult(false);
            callbackError = Catch.Exception(() =>
            {
                _completed.Task.WaitAsync(Deadline).GetAwaiter().GetResult();
                _watchToken.WaitHandle.WaitOne(0).ShouldBeTrue();
            });
        });

        var disposalError = await Catch.Exception(() => Task.Run(() => ((IDisposable)_observable).Dispose()).WaitAsync(Deadline));

        disposalError.ShouldBeNull();
        callbackExecuted.ShouldBeTrue();
        callbackError.ShouldBeNull();
    }
}
