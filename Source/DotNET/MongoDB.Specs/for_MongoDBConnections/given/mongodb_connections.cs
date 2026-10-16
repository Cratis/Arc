// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoDBConnections.given;

public class mongodb_connections : Specification
{
    protected MongoDBOptions _options;
    protected IConnectionResolvers _resolvers;
    protected ICachingConnectionResolver _resolver;
    protected IMongoServerResolver _serverResolver;
    protected IMongoDatabaseNameResolver _databaseNameResolver;
    protected ITenantIdAccessor _tenantIdAccessor;
    protected IMongoDBClientFactory _clientFactory;
    protected IMongoClient _client;
    protected MongoClientSettings? _createdWith;
    protected MongoDBConnections _connections;

    void Establish()
    {
        _options = new() { Server = "mongodb://configured:27017", Database = "app" };
        _resolver = Substitute.For<ICachingConnectionResolver>();
        _resolver.Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>()).Returns(ResolvedConnection.Unchanged);
        _resolvers = Substitute.For<IConnectionResolvers>();
        _resolvers.For(Arg.Any<ConnectionResolutionOptions>()).Returns(call => call.Arg<ConnectionResolutionOptions>().IsConfigured ? _resolver : null);
        _serverResolver = Substitute.For<IMongoServerResolver>();
        _serverResolver.Resolve().Returns(new MongoUrl(_options.Server));
        _databaseNameResolver = Substitute.For<IMongoDatabaseNameResolver>();
        _databaseNameResolver.Resolve().Returns("app+acme");
        _tenantIdAccessor = Substitute.For<ITenantIdAccessor>();
        _tenantIdAccessor.Current.Returns(new TenantId("acme"));
        _client = Substitute.For<IMongoClient>();
        _clientFactory = Substitute.For<IMongoDBClientFactory>();
        _clientFactory.Create().Returns(_client);
        _clientFactory.Create(Arg.Do<MongoClientSettings>(_ => _createdWith = _)).Returns(_client);
        _connections = new(Options.Create(_options), _resolvers, _serverResolver, _databaseNameResolver, _tenantIdAccessor, _clientFactory);
    }
}
