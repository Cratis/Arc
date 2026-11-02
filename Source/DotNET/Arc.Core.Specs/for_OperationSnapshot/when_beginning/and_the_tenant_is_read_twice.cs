// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.for_OperationSnapshot.when_beginning;

public class and_the_tenant_is_read_twice : given.an_operation_snapshot_environment
{
    TenantId _first;
    TenantId _second;

    void Because()
    {
        using var snapshot = OperationSnapshot.Begin(_services, () => null);
        _first = snapshot.Tenant;
        _second = snapshot.Tenant;
    }

    [Fact] void should_resolve_the_tenant() => _first.ShouldEqual(new TenantId("tenant-1"));
    [Fact] void should_return_the_same_tenant() => _second.ShouldEqual(_first);
    [Fact] void should_resolve_only_once() => _resolver.Received(1).Resolve();
}
