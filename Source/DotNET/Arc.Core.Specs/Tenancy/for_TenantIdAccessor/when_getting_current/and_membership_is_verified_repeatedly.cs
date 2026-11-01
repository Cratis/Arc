// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_membership_is_verified_repeatedly : given.a_verified_tenant_id_accessor
{
    void Because()
    {
        _ = _accessor.Current;
        _ = _accessor.Current;
        _ = _accessor.Current;
    }

    [Fact] void should_resolve_the_tenant_every_time() => _resolver.Received(3).Resolve();
    [Fact] void should_check_membership_every_time() => _membership.Received(3).IsMember(_principal, new TenantId("selected"));
    [Fact] void should_resolve_memberships_once() => _request.RequestServices.Received(1).GetService(typeof(IEnumerable<ITenantMembership>));
}
