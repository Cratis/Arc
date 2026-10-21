// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBJoinedObserveBuilder.when_selecting;

public class and_a_replay_collection_is_renamed_into_place : given.a_joined_observe_builder
{
    IDisposable _subscription;
    int _latestCount;

    async Task Because()
    {
        var initial = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _subscription = _builder.Select((a, _) => a.Count()).Subscribe(count =>
        {
            _latestCount = count;
            if (count == 2) initial.TrySetResult();
            if (count == 3) refreshed.TrySetResult();
        });
        await initial.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _docs1.Add(new DocumentA("Replayed"));
        _databaseChanges.OnNext(new ChangeStreamDocument<BsonDocument>(
            new BsonDocument
            {
                { "operationType", "rename" },
                { "ns", new BsonDocument { { "db", DatabaseName }, { "coll", "replay_documenta" } } },
                { "to", new BsonDocument { { "db", DatabaseName }, { "coll", Collection1Name } } }
            },
            BsonDocumentSerializer.Instance));
        await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_publish_a_fresh_target_collection_snapshot() => _latestCount.ShouldEqual(3);

    void Destroy() => _subscription.Dispose();
}
