// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions;

public class when_the_watch_terminates : given.a_controlled_watch
{
    [Theory]
    [InlineData("single")]
    [InlineData("by_id")]
    [InlineData("list")]
    public async Task should_complete_the_observable_and_leave_the_shared_stream(string kind)
    {
        await StartObserving(kind);

        // The shared stream completes when the watcher is disposed; that is normal completion, never an error.
        _databaseChanges.Complete();

        await _completed.Task.WaitAsync(Deadline);
        _databaseChanges.Subscribers.ShouldEqual(0);
        Catch.Exception(_subscription.Dispose).ShouldBeNull();
    }

    [Theory]
    [InlineData("single")]
    [InlineData("by_id")]
    [InlineData("list")]
    public async Task should_error_the_observable_and_leave_the_shared_stream(string kind)
    {
        await StartObserving(kind);
        var failure = new Exception("The change stream failed.");

        _databaseChanges.Fail(failure);

        var error = await Catch.Exception(() => _completed.Task.WaitAsync(Deadline));
        error.ShouldBeSame(failure);
        _databaseChanges.Subscribers.ShouldEqual(0);
        Catch.Exception(_subscription.Dispose).ShouldBeNull();
    }
}
