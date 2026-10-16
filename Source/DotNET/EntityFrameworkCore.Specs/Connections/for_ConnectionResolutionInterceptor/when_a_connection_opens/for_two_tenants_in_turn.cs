// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.EntityFrameworkCore.Connections.for_ConnectionResolutionInterceptor.when_a_connection_opens;

public class for_two_tenants_in_turn : given.an_interceptor
{
    string _first;
    string _second;

    async Task Because()
    {
        await _interceptor.ConnectionOpeningAsync(_connection, null!, default);
        _first = _connection.DataSource;

        _tenantIdAccessor.Current.Returns(new TenantId("globex"));
        await _interceptor.ConnectionOpeningAsync(_connection, null!, default);
        _second = _connection.DataSource;
    }

    [Fact] void should_resolve_for_sqlite() => _requests[0].Storage.ShouldEqual(WellKnownConnectionStorage.Sqlite);
    [Fact] void should_resolve_for_runtime() => _requests[0].Purpose.ShouldEqual(ConnectionPurpose.Runtime);
    [Fact] void should_use_the_first_tenants_database() => _first.ShouldEqual("acme.db");
    [Fact] void should_use_the_second_tenants_database_on_the_same_connection() => _second.ShouldEqual("globex.db");
}
