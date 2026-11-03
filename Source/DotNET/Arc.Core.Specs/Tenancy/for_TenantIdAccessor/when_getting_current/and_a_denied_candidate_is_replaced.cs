// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_a_denied_candidate_is_replaced : given.a_verified_tenant_id_accessor
{
    Exception _denied;
    TenantId _allowed;
    void Because()
    {
        _membership.IsMember(_principal, new TenantId("selected")).Returns(false);
        _denied = Catch.Exception(() => _ = _accessor.Current);
        _resolver.Resolve().Returns("allowed");
        _allowed = _accessor.Current;
    }
    [Fact] void should_deny_the_first_candidate() => _denied.ShouldBeOfExactType<TenantMembershipDenied>();
    [Fact] void should_resolve_and_verify_the_next_candidate() => _allowed.ShouldEqual(new TenantId("allowed"));
}
