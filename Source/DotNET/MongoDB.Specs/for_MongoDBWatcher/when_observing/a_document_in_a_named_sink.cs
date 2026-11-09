// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.MongoDB.Sinks;
using Cratis.Arc.Sinks;
using Cratis.Serialization;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBWatcher.when_observing;

public class a_document_in_a_named_sink : given.watched_databases
{
    IMongoClient _defaultClient;
    IMongoClient _sinkClient;
    Task _sinkWatchStarted;
    IMongoDBSinks _sinks;
    MongoDBWatcher _sinkAwareWatcher;

    void Establish()
    {
        var namingPolicy = Substitute.For<INamingPolicy>();
        namingPolicy.GetReadModelName(Arg.Any<Type>()).Returns(ci => ci.Arg<Type>().Name.ToLowerInvariant());
        DatabaseExtensions.SetNamingPolicy(namingPolicy);

        (_defaultClient, _, _) = DatabaseOn("default:27017");
        (_sinkClient, _, _sinkWatchStarted) = DatabaseOn("reporting:27017");
        _clientFactory.Create().Returns(_defaultClient);
        _sinks = Substitute.For<IMongoDBSinks>();
        _sinks.GetClientFor("reporting").Returns(_sinkClient);
        var readModelSinks = Substitute.For<IReadModelSinks>();
        readModelSinks.GetSinkNameFor(typeof(string)).Returns("reporting");
        var databaseNameResolver = Substitute.For<IMongoDatabaseNameResolver>();
        databaseNameResolver.Resolve().Returns(DatabaseName);
        _sinkAwareWatcher = new MongoDBWatcher(
            _clientFactory,
            databaseNameResolver,
            Substitute.For<Microsoft.Extensions.Logging.ILogger<MongoDBWatcher>>(),
            null,
            readModelSinks,
            _sinks);
    }

    async Task Because()
    {
        _ = _sinkAwareWatcher.Observe<string>();
        await _sinkWatchStarted.WaitAsync(Deadline, global::Xunit.TestContext.Current.CancellationToken);
    }

    void Destroy() => _sinkAwareWatcher.Dispose();

    [Fact] void should_watch_the_database_on_the_sink_server() => _sinkClient.Received(1).GetDatabase(DatabaseName, Arg.Any<MongoDatabaseSettings>());
    [Fact] void should_not_watch_on_the_default_server() => _defaultClient.DidNotReceive().GetDatabase(Arg.Any<string>(), Arg.Any<MongoDatabaseSettings>());
}
