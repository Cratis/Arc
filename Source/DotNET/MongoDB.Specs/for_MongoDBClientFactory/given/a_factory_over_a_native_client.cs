// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Metrics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Arc.MongoDB.for_MongoDBClientFactory.given;

public class a_factory_over_a_native_client : Specification
{
    protected readonly MongoCommandException _collectionNotFound = new(new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))), "Collection not found", new BsonDocument());
    protected IMongoClient _native;
    protected IMongoDatabase _nativeDatabase;
    protected IMongoCollection<Document> _nativeCollection;
    protected MongoDBOptions _options;
    protected MongoClientSettings _settings;
    protected MongoDBClientFactory _factory;

    void Establish()
    {
        _native = Substitute.For<IMongoClient>();
        _nativeDatabase = Substitute.For<IMongoDatabase>();
        _nativeCollection = Substitute.For<IMongoCollection<Document>>();
        _native.Settings.Returns(new MongoClientSettings { MaxConnectionPoolSize = 4 });
        _native.GetDatabase("db", Arg.Any<MongoDatabaseSettings>()).Returns(_nativeDatabase);
        _nativeDatabase.GetCollection<Document>("docs", Arg.Any<MongoCollectionSettings>()).Returns(_nativeCollection);
        _options = new MongoDBOptions { Server = "mongodb://localhost:27017", Database = "db" };
        _settings = MongoClientSettings.FromConnectionString(_options.Server);
        var serverResolver = Substitute.For<IMongoServerResolver>();
        serverResolver.Resolve().Returns(new MongoUrl(_options.Server));
        _factory = new MongoDBClientFactory(serverResolver, Substitute.For<IMeter<IMongoClient>>(), Options.Create(_options), NullLogger<MongoDBClientFactory>.Instance, _ => _native);
    }
}
