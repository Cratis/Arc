// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;

namespace Cratis.Arc.EntityFrameworkCore.Connections.for_ConnectionResolutionInterceptor.when_a_connection_opens;

public class in_a_schema_scope : given.an_interceptor
{
    void Because()
    {
        using var scope = ConnectionPurposeScope.Begin(ConnectionPurpose.Schema);
        _interceptor.ConnectionOpening(_connection, null!, default);
    }

    [Fact] void should_resolve_for_schema() => _requests[0].Purpose.ShouldEqual(ConnectionPurpose.Schema);
}
