// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_MongoDBWatcher.when_observing;

public class and_mongodb_is_not_configured : Specification
{
    MongoDBWatcher _watcher;
    Exception _error;

    void Establish() => _watcher = new MongoDBWatcher(Substitute.For<Microsoft.Extensions.Logging.ILogger<MongoDBWatcher>>());

    void Because() => _error = Catch.Exception(() => _watcher.Observe<BsonDocument>());

    void Destroy() => _watcher.Dispose();

    [Fact] void should_say_what_is_missing() => _error.ShouldBeOfExactType<MongoDBWatcherNotConfigured>();
}
