// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions;

public class when_a_change_dispatch_fails : given.an_observed_collection
{
    [Theory]
    [InlineData("single")]
    [InlineData("by_id")]
    [InlineData("list")]
    public async Task should_error_the_observable_instead_of_losing_the_change(string kind)
    {
        var deadline = TimeSpan.FromSeconds(5);
        var failure = new Exception("The re-check failed.");
        _collection
            .FindAsync(
                Arg.Any<FilterDefinition<ObservedDocument>>(),
                Arg.Any<FindOptions<ObservedDocument, BsonDocument>>(),
                Arg.Any<CancellationToken>())
            .Returns<Task<IAsyncCursor<BsonDocument>>>(_ => throw failure);
        IObservable<object> observable = kind switch
        {
            "single" => _collection.ObserveSingle(),
            "by_id" => _collection.ObserveById(_documents[0].Id),
            _ => _collection.Observe()
        };
        var initial = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = false;
        using var subscription = observable.Subscribe(_ => initial.TrySetResult(), exception => error.TrySetResult(exception), () => completed = true);
        _initialQueryGate.SetResult();
        await initial.Task.WaitAsync(deadline);

        PushChange(UpdateOf(_documents[0]));

        (await error.Task.WaitAsync(deadline)).ShouldBeSame(failure);
        completed.ShouldBeFalse();
    }
}
