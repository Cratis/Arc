// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_using_an_authorized_tenant;

public class and_the_selected_tenant_is_not_set : given.a_tenant_id_accessor
{
    TenantId _inside;
    TenantId _after;

    void Establish() => _tenantIdResolver.Resolve().Returns("request-tenant");

    void Because()
    {
        using (_accessor.UseAuthorizedTenant(TenantId.NotSet))
        {
            _inside = _accessor.Current;
        }
        _after = _accessor.Current;
    }

    [Fact] void should_keep_the_authorized_tenant_unset() => _inside.ShouldEqual(TenantId.NotSet);
    [Fact] void should_restore_request_resolution() => _after.ShouldEqual(new TenantId("request-tenant"));
    [Fact] void should_only_resolve_after_the_scope_ends() => _tenantIdResolver.Received(1).Resolve();
}
