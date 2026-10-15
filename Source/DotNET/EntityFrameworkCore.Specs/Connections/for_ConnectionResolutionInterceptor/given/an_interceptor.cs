// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.Tenancy;
using Microsoft.Data.Sqlite;

namespace Cratis.Arc.EntityFrameworkCore.Connections.for_ConnectionResolutionInterceptor.given;

public class an_interceptor : Specification
{
    protected ICachingConnectionResolver _resolver;
    protected ITenantIdAccessor _tenantIdAccessor;
    protected List<ConnectionRequest> _requests = [];
    protected SqliteConnection _connection;
    protected ConnectionResolutionInterceptor _interceptor;

    void Establish()
    {
        _resolver = Substitute.For<ICachingConnectionResolver>();
        _resolver.Resolve(Arg.Do<ConnectionRequest>(_requests.Add), Arg.Any<CancellationToken>())
            .Returns(call => new ResolvedConnection(Database: $"{call.Arg<ConnectionRequest>().Scope[WellKnownConnectionScopeKeys.Tenant]}.db"));
        _tenantIdAccessor = Substitute.For<ITenantIdAccessor>();
        _tenantIdAccessor.Current.Returns(new TenantId("acme"));
        _connection = new("Data Source=configured.db");
        _interceptor = new(_resolver, _tenantIdAccessor);
    }

    void Destroy() => _connection.Dispose();
}
