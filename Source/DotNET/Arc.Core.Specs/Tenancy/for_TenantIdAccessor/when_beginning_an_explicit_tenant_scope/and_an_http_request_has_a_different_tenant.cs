// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_beginning_an_explicit_tenant_scope;

public class and_an_http_request_has_a_different_tenant : for_HeaderTenantIdResolver.given.a_header_tenant_id_resolver
{
    TenantId _during;
    TenantId _after;

    void Establish() => _headers[_options.Value.Tenancy.HttpHeader] = "request-tenant";

    void Because()
    {
        var accessor = new TenantIdAccessor(_resolver);
        using (accessor.Begin("explicit"))
        {
            _during = accessor.Current;
        }

        _after = accessor.Current;
    }

    [Fact] void should_prefer_the_explicit_tenant_over_the_http_tenant() => _during.ShouldEqual(new TenantId("explicit"));
    [Fact] void should_restore_the_http_tenant_after_disposal() => _after.ShouldEqual(new TenantId("request-tenant"));
}
