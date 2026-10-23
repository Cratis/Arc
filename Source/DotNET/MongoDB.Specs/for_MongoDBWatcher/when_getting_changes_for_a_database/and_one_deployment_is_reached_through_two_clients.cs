// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBWatcher.when_getting_changes_for_a_database;

public class and_one_deployment_is_reached_through_two_clients : given.watched_databases
{
    IMongoClient _firstClient;
    IMongoClient _secondClient;
    IObservable<ChangeStreamDocument<BsonDocument>> _first;
    IObservable<ChangeStreamDocument<BsonDocument>> _second;

    async Task Because()
    {
        (_firstClient, var firstDatabase, var watchStarted) = DatabaseOn("shared-server:27017");
        (_secondClient, var secondDatabase, _) = DatabaseOn("shared-server:27017");
        _first = _watcher.GetChanges(firstDatabase);
        _second = _watcher.GetChanges(secondDatabase);
        await watchStarted.WaitAsync(Deadline);
    }

    [Fact] void should_share_one_stream() => Assert.Same(_first, _second);
    [Fact] void should_not_open_a_stream_with_the_second_client() => _secondClient.DidNotReceive().GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>());
}
