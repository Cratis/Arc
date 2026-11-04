// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.EntityFrameworkCore.Connections.for_ConnectionResolutionInterceptor.when_a_connection_opens;

public class in_a_connection_tenant_scope : given.an_interceptor
{
    void Because()
    {
        using var scope = ConnectionTenantScope.Begin(new TenantId("globex"));
        _interceptor.ConnectionOpening(_connection, null!, default);
    }

    [Fact] void should_use_the_scoped_tenants_database() => _connection.DataSource.ShouldEqual("globex.db");
    [Fact] void should_resolve_for_the_scoped_tenant() => _requests[0].Scope[WellKnownConnectionScopeKeys.Tenant].ShouldEqual("globex");
}
