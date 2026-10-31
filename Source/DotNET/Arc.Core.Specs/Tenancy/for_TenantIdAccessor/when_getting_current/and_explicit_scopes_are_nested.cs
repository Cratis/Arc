// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_explicit_scopes_are_nested : given.a_verified_tenant_id_accessor
{
    TenantId _inner;
    TenantId _outer;
    void Because()
    {
        using (_accessor.Begin(new TenantId("outer")))
        {
            using (_accessor.Begin(new TenantId("inner")))
            {
                _inner = _accessor.Current;
            }
            _outer = _accessor.Current;
        }
    }
    [Fact] void should_restore_the_outer_scope() => _outer.ShouldEqual(new TenantId("outer"));
    [Fact] void should_use_the_innermost_scope() => _inner.ShouldEqual(new TenantId("inner"));
    [Fact] void should_not_call_membership() => _membership.DidNotReceive().IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>());
}
