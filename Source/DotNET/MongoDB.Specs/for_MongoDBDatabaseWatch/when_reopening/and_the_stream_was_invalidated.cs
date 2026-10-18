// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Serializers;

namespace Cratis.Arc.MongoDB.for_MongoDBDatabaseWatch.when_reopening;

public class and_the_stream_was_invalidated : given.a_database_watch
{
    async Task Because()
    {
        _worker = _watch.Run("testdb");
        await _delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _firstCursor.Current.Returns([new(new BsonDocument("operationType", "invalidate"), BsonDocumentSerializer.Instance)]);
        _nextMove.SetResult(true);
        await _resynchronized.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_reopen_without_the_old_token() => _options[1].ResumeAfter.ShouldBeNull();
    [Fact] void should_release_the_invalidated_cursor() => _firstCursor.Received(1).Dispose();
    [Fact] void should_keep_the_new_stream_open() => _secondCursor.DidNotReceive().Dispose();
}
