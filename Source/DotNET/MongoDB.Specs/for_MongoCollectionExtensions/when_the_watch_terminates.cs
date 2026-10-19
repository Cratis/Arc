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
    [InlineData("single", "disposed")]
    [InlineData("by_id", "disposed")]
    [InlineData("list", "disposed")]
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
            case "disposed":
                _nextBatch.SetException(new ObjectDisposedException("change stream"));
                break;
        }

        // Normal completion and cancellation must never be reported as an error.
        await Task.WhenAll(_completed.Task, cancelled.Task).WaitAsync(Deadline);
        _watchToken.IsCancellationRequested.ShouldBeTrue();
        Catch.Exception(_subscription.Dispose).ShouldBeNull();
    }

    [Theory]
    [InlineData("single")]
    [InlineData("by_id")]
    [InlineData("list")]
    public async Task should_error_the_observable_and_stop_its_lifetime(string kind)
    {
        await StartObserving(kind);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = _watchToken.Register(() => cancelled.TrySetResult());
        var failure = new Exception("The change stream failed.");

        _nextBatch.SetException(failure);

        var error = await Catch.Exception(() => _completed.Task.WaitAsync(Deadline));
        error.ShouldBeSame(failure);
        await cancelled.Task.WaitAsync(Deadline);
        _watchToken.IsCancellationRequested.ShouldBeTrue();
        Catch.Exception(_subscription.Dispose).ShouldBeNull();
    }
}
