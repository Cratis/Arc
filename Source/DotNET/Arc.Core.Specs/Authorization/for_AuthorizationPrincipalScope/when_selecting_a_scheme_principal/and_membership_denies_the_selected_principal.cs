// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Authorization.for_AuthorizationPrincipalScope.when_selecting_a_scheme_principal;

public class and_membership_denies_the_selected_principal : Tenancy.for_TenantIdAccessor.given.a_verified_tenant_id_accessor
{
    Exception _exception;
    ClaimsPrincipal _selected;
    TenantId _subsequent;
    void Because()
    {
        _selected = new ClaimsPrincipal(new ClaimsIdentity("scheme"));
        _membership.IsMember(_selected, new TenantId("selected")).Returns(false);
        var services = Substitute.For<IServiceProvider>();
        var scope = new AuthorizationPrincipalScope(_principals, new ArcAuthorizationPolicyRuntime([]), _accessor, _resolver);
        using var boundary = AuthorizationExecutionScopes.Begin(services);
        _exception = Catch.Exception(() => scope.Begin(_selected, services));
        _subsequent = _accessor.Current;
    }
    [Fact] void should_deny_before_creating_a_lease() => _exception.ShouldBeOfExactType<TenantMembershipDenied>();
    [Fact] void should_restore_the_request_principal() => _principals.Current.ShouldBeSame(_principal);
    [Fact] void should_leave_the_tenant_cache_unchanged() => _accessor.Cached.ShouldBeNull();
    [Fact] void should_allow_a_later_read_for_the_original_principal() => _subsequent.ShouldEqual(new TenantId("selected"));
}
