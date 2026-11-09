// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Sinks;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Sinks.for_MongoDBReadModelDatabases.given;

public class read_model_databases : Specification
{
    protected IMongoClient _defaultClient;
    protected IMongoDatabase _defaultDatabase;
    protected IMongoClient _sinkClient;
    protected IMongoDatabase _sinkDatabase;
    protected IReadModelSinks _readModelSinks;
    protected IMongoDBSinks _sinks;
    protected MongoDBReadModelDatabases _databases;

    void Establish()
    {
        _defaultClient = Substitute.For<IMongoClient>();
        _defaultDatabase = Substitute.For<IMongoDatabase>();
        _sinkClient = Substitute.For<IMongoClient>();
        _sinkDatabase = Substitute.For<IMongoDatabase>();
        _sinkClient.GetDatabase("store+acme", Arg.Any<MongoDatabaseSettings>()).Returns(_sinkDatabase);
        _readModelSinks = Substitute.For<IReadModelSinks>();
        _sinks = Substitute.For<IMongoDBSinks>();
        _sinks.GetClientFor("reporting").Returns(_sinkClient);
        _databases = new(new MongoDBConnection(_defaultClient, "store+acme"), _defaultDatabase, _readModelSinks, _sinks);
    }
}
