// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBWatcher.when_getting_changes_for_a_database;

public class and_it_belongs_to_another_client : given.watched_databases
{
    IMongoClient _client;
    IMongoDatabase _database;

    async Task Because()
    {
        (_client, _database, var watchStarted) = DatabaseOn("storage-server:27017");
        _watcher.GetChanges(_database);
        await watchStarted.WaitAsync(Deadline);
    }

    [Fact] void should_open_the_stream_with_the_databases_own_client() => _client.Received(1).GetDatabase(DatabaseName, Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_not_use_the_configured_client() => _clientFactory.DidNotReceive().Create();
}
