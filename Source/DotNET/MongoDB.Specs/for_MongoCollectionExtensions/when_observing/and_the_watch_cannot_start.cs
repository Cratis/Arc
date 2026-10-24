// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_the_watch_cannot_start : given.an_observed_collection
{
    [Theory]
    [InlineData("single")]
    [InlineData("by_id")]
    [InlineData("list")]
    public async Task should_report_the_failure_without_emitting_or_completing(string kind)
    {
        var deadline = TimeSpan.FromSeconds(5);
        var failure = new Exception("MongoDB is unreachable.");
        _watcher.GetChanges(_database).Returns<IObservable<ChangeStreamDocument<BsonDocument>>>(_ => throw failure);
        IObservable<object> observable = kind switch
        {
            "single" => _collection.ObserveSingle(),
            "by_id" => _collection.ObserveById(_documents[0].Id),
            _ => _collection.Observe()
        };
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var emissions = 0;
        var completed = false;
        using var subscription = observable.Subscribe(_ => emissions++, exception => error.TrySetResult(exception), () => completed = true);

        (await error.Task.WaitAsync(deadline, global::Xunit.TestContext.Current.CancellationToken)).ShouldBeSame(failure);
        emissions.ShouldEqual(0);
        completed.ShouldBeFalse();
    }
}
