// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_membership_changes_between_reads : given.a_verified_tenant_id_accessor
{
    TenantId _first;
    Exception _exception;
    void Because()
    {
        _first = _accessor.Current;
        _membership.IsMember(_principal, new TenantId("selected")).Returns(false);
        _exception = Catch.Exception(() => _ = _accessor.Current);
    }
    [Fact] void should_allow_the_first_read() => _first.ShouldEqual(new TenantId("selected"));
    [Fact] void should_recheck_membership() => _exception.ShouldBeOfExactType<TenantMembershipDenied>();
    [Fact] void should_not_cache_denial() => _accessor.Cached.ShouldBeNull();
}
