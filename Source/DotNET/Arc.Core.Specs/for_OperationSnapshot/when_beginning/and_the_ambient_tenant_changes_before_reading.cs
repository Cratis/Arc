// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.for_OperationSnapshot.when_beginning;

public class and_the_ambient_tenant_changes_before_reading : given.an_operation_snapshot_environment
{
    TenantId _tenant;

    void Because()
    {
        _ = _accessor.Current;
        using var snapshot = OperationSnapshot.Begin(_services, () => null);
        ITenantScope scope = _accessor;
        using (scope.Begin("other"))
        {
            _tenant = snapshot.Tenant;
        }
    }

    [Fact] void should_return_the_tenant_at_begin() => _tenant.ShouldEqual(new TenantId("tenant-1"));
}
