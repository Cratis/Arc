// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBJoinedObserveBuilderWithThreeCollections.when_selecting;

public class and_the_database_is_resynchronized : given.a_joined_observe_builder
{
    IDisposable _subscription;
    (int A, int B, int C) _latest;

    async Task Because()
    {
        var initial = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _subscription = _builder.Select((a, b, c) => (a.Count(), b.Count(), c.Count())).Subscribe(counts =>
        {
            _latest = counts;
            if (counts == (2, 2, 2)) initial.TrySetResult();
            if (counts == (0, 0, 0)) refreshed.TrySetResult();
        });
        await initial.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _docs1.Clear();
        _docs2.Clear();
        _docs3.Clear();
        _databaseChanges.OnNext(new ChangeStreamDocument<BsonDocument>(new BsonDocument("operationType", "invalidate"), BsonDocumentSerializer.Instance));
        await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_refresh_every_joined_source() => _latest.ShouldEqual((0, 0, 0));

    void Destroy() => _subscription.Dispose();
}
