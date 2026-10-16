// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Connections.for_ConnectionRequest.when_creating_for_a_tenant;

public class and_the_tenant_is_named : Specification
{
    ConnectionRequest _result;

    void Because() => _result = ConnectionRequest.ForTenant(WellKnownConnectionStorage.SqlServer, ConnectionPurpose.Runtime, new TenantId("acme"));

    [Fact] void should_have_the_tenant_in_the_scope() => _result.Scope[WellKnownConnectionScopeKeys.Tenant].ShouldEqual("acme");
    [Fact] void should_have_the_storage() => _result.Storage.ShouldEqual(WellKnownConnectionStorage.SqlServer);
}
