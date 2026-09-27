// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_beginning_an_explicit_tenant_scope;

public class and_the_tenant_was_cached : given.a_tenant_id_accessor
{
    TenantId _before;
    TenantId _inside;
    TenantId _after;

    void Establish()
    {
        _tenantIdResolver.Resolve().Returns("original");
        _before = _accessor.Current;
    }

    void Because()
    {
        ITenantScope tenants = _accessor;
        using (tenants.Begin("explicit"))
        {
            _inside = _accessor.Current;
        }

        _after = _accessor.Current;
    }

    [Fact] void should_override_the_cached_tenant() => _inside.ShouldEqual(new TenantId("explicit"));
    [Fact] void should_restore_the_cached_tenant() => _after.ShouldEqual(_before);
}
