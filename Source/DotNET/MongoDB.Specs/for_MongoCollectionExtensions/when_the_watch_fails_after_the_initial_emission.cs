// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions;

public class when_the_watch_fails_after_the_initial_emission : given.a_controlled_watch
{
    [Theory]
    [InlineData("single")]
    [InlineData("by_id")]
    [InlineData("list")]
    public async Task should_deliver_the_initial_emission_then_the_error_and_never_complete(string kind)
    {
        await StartObserving(kind);
        var notifications = new List<string>();
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var observer = _observable.Subscribe(
            _ => notifications.Add("next"),
            exception =>
            {
                notifications.Add("error");
                error.TrySetResult(exception);
            },
            () => notifications.Add("completed"));
        var failure = new Exception("The change stream failed after the first emission.");

        _databaseChanges.Fail(failure);

        (await error.Task.WaitAsync(Deadline, global::Xunit.TestContext.Current.CancellationToken)).ShouldBeSame(failure);
        notifications.ShouldEqual(["next", "error"]);
    }
}
