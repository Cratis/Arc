// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Connections.for_ConnectionRequest.when_creating_for_a_tenant;

public class and_the_tenant_is_not_set : Specification
{
    ConnectionRequest _result;

    void Because() => _result = ConnectionRequest.ForTenant(WellKnownConnectionStorage.MongoDB, ConnectionPurpose.Runtime, TenantId.NotSet);

    [Fact] void should_use_the_default_tenant() => _result.Scope[WellKnownConnectionScopeKeys.Tenant].ShouldEqual(TenantId.Default.Value);
}
