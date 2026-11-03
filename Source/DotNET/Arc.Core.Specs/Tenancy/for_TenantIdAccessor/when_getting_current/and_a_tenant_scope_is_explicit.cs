// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_a_tenant_scope_is_explicit : given.a_verified_tenant_id_accessor
{
    TenantId _result;
    void Because()
    {
        using var scope = _accessor.Begin(new TenantId("trusted"));
        _result = _accessor.Current;
    }
    [Fact] void should_return_the_trusted_tenant() => _result.ShouldEqual(new TenantId("trusted"));
    [Fact] void should_not_resolve_a_candidate() => _resolver.DidNotReceive().Resolve();
    [Fact] void should_not_call_membership() => _membership.DidNotReceive().IsMember(Arg.Any<ClaimsPrincipal>(), Arg.Any<TenantId>());
}
