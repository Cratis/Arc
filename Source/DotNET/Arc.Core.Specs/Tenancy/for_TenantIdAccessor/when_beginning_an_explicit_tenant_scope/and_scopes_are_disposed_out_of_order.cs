// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_beginning_an_explicit_tenant_scope;

public class and_scopes_are_disposed_out_of_order : given.a_tenant_id_accessor
{
    TenantId _whileInnerIsActive;
    TenantId _afterOuterIsDisposed;
    TenantId _afterInnerIsDisposed;

    void Establish() => _tenantIdResolver.Resolve().Returns("request-tenant");

    void Because()
    {
        var outer = _accessor.Begin("acme");
        var inner = _accessor.Begin("globex");
        _whileInnerIsActive = _accessor.Current;
        outer.Dispose();
        _afterOuterIsDisposed = _accessor.Current;
        inner.Dispose();
        _afterInnerIsDisposed = _accessor.Current;
    }

    [Fact] void should_select_the_inner_tenant() => _whileInnerIsActive.ShouldEqual(new TenantId("globex"));
    [Fact] void should_leave_the_inner_tenant_selected_when_the_outer_scope_is_disposed() => _afterOuterIsDisposed.ShouldEqual(new TenantId("globex"));
    [Fact] void should_restore_the_resolver_instead_of_the_disposed_outer_tenant() => _afterInnerIsDisposed.ShouldEqual(new TenantId("request-tenant"));
}
