// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_a_new_request_follows_a_denial : given.a_verified_tenant_id_accessor
{
    TenantId _result;
    void Because()
    {
        _membership.IsMember(_principal, new TenantId("selected")).Returns(false);
        using (TenantIdAccessor.Independent.BeginRequest())
        {
            Catch.Exception(() => _ = _accessor.Current).ShouldBeOfExactType<TenantMembershipDenied>();
        }
        _resolver.Resolve().Returns("next-request");
        using (TenantIdAccessor.Independent.BeginRequest())
        {
            _result = _accessor.Current;
        }
    }
    [Fact] void should_verify_the_new_request_independently() => _result.ShouldEqual(new TenantId("next-request"));
    [Fact] void should_leave_no_cached_candidate() => _accessor.Cached.ShouldBeNull();
}
