// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.for_OperationSnapshot.when_nesting;

public class and_an_inner_frame_begins : given.an_operation_snapshot_environment
{
    OperationSnapshot _outer;
    OperationSnapshot _inner;
    TenantId _innerTenant;
    TenantId _outerTenant;
    OperationSnapshot _currentAfterInner;

    void Because()
    {
        _outer = OperationSnapshot.Begin(_services, () => null);
        ITenantScope scope = _accessor;
        using (scope.Begin("inner-tenant"))
        {
            _inner = OperationSnapshot.Begin(_services, () => null);
            _innerTenant = _inner.Tenant;
            _inner.Dispose();
        }

        _currentAfterInner = OperationSnapshot.Current;
        _outerTenant = _outer.Tenant;
        _outer.Dispose();
    }

    [Fact] void should_give_the_inner_frame_the_then_current_tenant() => _innerTenant.ShouldEqual(new TenantId("inner-tenant"));
    [Fact] void should_make_the_outer_frame_current_again() => _currentAfterInner.ShouldEqual(_outer);
    [Fact] void should_keep_the_outer_tenant() => _outerTenant.ShouldEqual(new TenantId("tenant-1"));
    [Fact] void should_have_no_current_frame_afterwards() => OperationSnapshot.Current.ShouldBeNull();
}
