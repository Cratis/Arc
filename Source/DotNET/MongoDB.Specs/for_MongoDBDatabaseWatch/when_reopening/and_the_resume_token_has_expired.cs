// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Arc.MongoDB.for_MongoDBDatabaseWatch.when_reopening;

public class and_the_resume_token_has_expired : given.a_database_watch
{
    async Task Because()
    {
        _worker = _watch.Run("testdb");
        await _delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var connection = new ConnectionId(new ServerId(new ClusterId(), new System.Net.DnsEndPoint("localhost", 27017)));
        _nextMove.SetException(new MongoCommandException(connection, "history lost", new BsonDocument(), new BsonDocument("code", 286)));
        await _resynchronized.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_reopen_without_the_expired_token() => _options[1].ResumeAfter.ShouldBeNull();
    [Fact] void should_release_the_failed_cursor() => _firstCursor.Received(1).Dispose();
    [Fact] void should_resynchronize_observers_after_opening() => _options.Count.ShouldEqual(2);
}
