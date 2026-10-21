// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions;

public class when_the_watch_terminates : given.a_controlled_watch
{
    [Theory]
    [InlineData("single", "completed")]
    [InlineData("by_id", "completed")]
    [InlineData("list", "completed")]
    [InlineData("single", "failed")]
    [InlineData("by_id", "failed")]
    [InlineData("list", "failed")]
    public async Task should_complete_the_observable_and_leave_the_shared_stream(string kind, string termination)
    {
        await StartObserving(kind);

        if (termination == "completed")
        {
            _databaseChanges.Complete();
        }
        else
        {
            _databaseChanges.Fail(new Exception("The change stream failed."));
        }

        // A failed or completed shared stream has always been translated into observable completion.
        // An OnError, or a swallowed failure, is not completion.
        await _completed.Task.WaitAsync(Deadline);
        _databaseChanges.Subscribers.ShouldEqual(0);
        Catch.Exception(_subscription.Dispose).ShouldBeNull();
    }
}
