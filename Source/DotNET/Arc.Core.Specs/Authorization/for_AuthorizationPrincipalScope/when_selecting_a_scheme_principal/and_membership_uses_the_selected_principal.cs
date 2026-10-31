// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Authorization.for_AuthorizationPrincipalScope.when_selecting_a_scheme_principal;

public class and_membership_uses_the_selected_principal : Tenancy.for_TenantIdAccessor.given.a_verified_tenant_id_accessor
{
    ClaimsPrincipal _selected;
    TenantId _tenant;
    void Because()
    {
        _selected = new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant", "selected")], "scheme"));
        var services = Substitute.For<IServiceProvider>();
        var scope = new AuthorizationPrincipalScope(_principals, new ArcAuthorizationPolicyRuntime([]), _accessor, _resolver);
        using var boundary = AuthorizationExecutionScopes.Begin(services);
        using (scope.Begin(_selected, services))
        {
            _tenant = _accessor.Current;
        }
    }
    [Fact] void should_check_the_selected_principal() => _membership.Received(1).IsMember(_selected, new TenantId("selected"));
    [Fact] void should_expose_the_verified_tenant() => _tenant.ShouldEqual(new TenantId("selected"));
    [Fact] void should_restore_the_principal() => _principals.Current.ShouldBeSame(_principal);
    [Fact] void should_restore_the_tenant_cache() => _accessor.Cached.ShouldBeNull();
}
