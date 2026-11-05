// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.for_GraphConnections.given;

public class graph_connections : Specification
{
    protected Neo4jOptions _options;
    protected IConnectionResolvers _resolvers;
    protected ICachingConnectionResolver _resolver;
    protected IGraphDatabaseNameResolver _databaseNameResolver;
    protected ITenantIdAccessor _tenantIdAccessor;
    protected INeo4jDriverFactory _driverFactory;
    protected IDriver _driver;
    protected Uri? _createdForServer;
    protected IAuthTokenManager? _createdWith;
    protected GraphConnections _connections;

    void Establish()
    {
        _options = new() { Server = "bolt://configured:7687", EventStore = "Orders", UserName = "neo4j", Password = "configured-password" };
        _resolver = Substitute.For<ICachingConnectionResolver>();
        _resolver.Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>()).Returns(ResolvedConnection.Unchanged);
        _resolvers = Substitute.For<IConnectionResolvers>();
        _resolvers.For(Arg.Any<ConnectionResolutionOptions>()).Returns(call => call.Arg<ConnectionResolutionOptions>().IsConfigured ? _resolver : null);
        _databaseNameResolver = Substitute.For<IGraphDatabaseNameResolver>();
        _databaseNameResolver.Resolve().Returns("orders-acme");
        _tenantIdAccessor = Substitute.For<ITenantIdAccessor>();
        _tenantIdAccessor.Current.Returns(new TenantId("acme"));
        _driver = Substitute.For<IDriver>();
        _driverFactory = Substitute.For<INeo4jDriverFactory>();
        _driverFactory.GetConfiguredAuthentication().Returns(new BasicGraphAuthentication("neo4j", "configured-password"));
        _driverFactory.Create(Arg.Do<Uri>(_ => _createdForServer = _), Arg.Do<IAuthTokenManager>(_ => _createdWith = _)).Returns(_driver);
        _connections = new(Options.Create(_options), _resolvers, _databaseNameResolver, _tenantIdAccessor, _driverFactory);
    }
}
