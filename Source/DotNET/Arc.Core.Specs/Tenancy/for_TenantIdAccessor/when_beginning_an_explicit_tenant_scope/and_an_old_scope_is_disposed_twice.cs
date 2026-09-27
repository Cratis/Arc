// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_beginning_an_explicit_tenant_scope;

public class and_an_old_scope_is_disposed_twice : given.a_tenant_id_accessor
{
    TenantId _during;
    TenantId _after;

    void Establish() => _tenantIdResolver.Resolve().Returns("request-tenant");

    void Because()
    {
        var old = _accessor.Begin("acme");
        old.Dispose();
        using (_accessor.Begin("globex"))
        {
            old.Dispose();
            _during = _accessor.Current;
        }

        _after = _accessor.Current;
    }

    [Fact] void should_leave_the_new_tenant_active() => _during.ShouldEqual(new TenantId("globex"));
    [Fact] void should_restore_the_resolver_after_the_new_scope() => _after.ShouldEqual(new TenantId("request-tenant"));
}
