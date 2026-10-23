// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBWatcher.when_getting_changes_for_a_database;

public class and_its_name_exists_on_two_deployments : given.watched_databases
{
    IObservable<ChangeStreamDocument<BsonDocument>> _first;
    IObservable<ChangeStreamDocument<BsonDocument>> _second;

    async Task Because()
    {
        var (_, firstDatabase, firstWatchStarted) = DatabaseOn("first-server:27017");
        var (_, secondDatabase, secondWatchStarted) = DatabaseOn("second-server:27017");
        _first = _watcher.GetChanges(firstDatabase);
        _second = _watcher.GetChanges(secondDatabase);
        await Task.WhenAll(firstWatchStarted, secondWatchStarted).WaitAsync(Deadline);
    }

    [Fact] void should_watch_each_deployment_on_a_stream_of_its_own() => Assert.NotSame(_first, _second);
}
