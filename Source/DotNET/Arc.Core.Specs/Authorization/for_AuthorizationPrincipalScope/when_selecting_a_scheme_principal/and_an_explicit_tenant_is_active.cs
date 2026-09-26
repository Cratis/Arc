// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Authorization.for_AuthorizationPrincipalScope.when_selecting_a_scheme_principal;

public class and_an_explicit_tenant_is_active : Specification
{
    TenantIdAccessor _tenants;
    ITenantIdResolver _resolver;
    TenantId _during;
    TenantId _after;

    void Establish()
    {
        _resolver = Substitute.For<ITenantIdResolver>();
        _resolver.Resolve().Returns("scheme-tenant");
        _tenants = new TenantIdAccessor(_resolver);
    }

    void Because()
    {
        var services = Substitute.For<IServiceProvider>();
        var principalAccessor = new CurrentPrincipalAccessor(Substitute.For<IHttpRequestContextAccessor>());
        IAuthorizationPolicyRuntime runtime = new ArcAuthorizationPolicyRuntime([]);
        var scope = new AuthorizationPrincipalScope(principalAccessor, runtime, _tenants, _resolver);
        using (AuthorizationExecutionScopes.Begin(services))
        using (_tenants.Begin("explicit"))
        using (scope.Begin(new ClaimsPrincipal(new ClaimsIdentity("selected-scheme")), services))
        {
            _during = _tenants.Current;
        }

        _after = _tenants.Current;
    }

    [Fact] void should_keep_the_explicit_tenant_during_scheme_authorization() => _during.ShouldEqual(new TenantId("explicit"));
    [Fact] void should_restore_the_resolved_tenant_after_the_scope() => _after.ShouldEqual(new TenantId("scheme-tenant"));
}
