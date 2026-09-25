// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions;

public class when_the_watch_terminates : given.a_controlled_watch
{
    [Theory]
    [InlineData("single", "completed")]
    [InlineData("by_id", "completed")]
    [InlineData("list", "completed")]
    [InlineData("single", "cancelled")]
    [InlineData("by_id", "cancelled")]
    [InlineData("list", "cancelled")]
    [InlineData("single", "failed")]
    [InlineData("by_id", "failed")]
    [InlineData("list", "failed")]
    public async Task should_complete_the_observable_and_stop_its_lifetime(string kind, string termination)
    {
        await StartObserving(kind);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = _watchToken.Register(() => cancelled.TrySetResult());

        switch (termination)
        {
            case "completed":
                _nextBatch.SetResult(false);
                break;
            case "cancelled":
                _nextBatch.SetCanceled();
                break;
            default:
                _nextBatch.SetException(new Exception("The change stream failed."));
                break;
        }

        // Watch has always translated cursor cancellation and failure into observable completion.
        // Both signals must arrive; a swallowed callback failure or an OnError is not completion.
        await Task.WhenAll(_completed.Task, cancelled.Task).WaitAsync(Deadline);
        _watchToken.IsCancellationRequested.ShouldBeTrue();
        Catch.Exception(_subscription.Dispose).ShouldBeNull();
    }
}
