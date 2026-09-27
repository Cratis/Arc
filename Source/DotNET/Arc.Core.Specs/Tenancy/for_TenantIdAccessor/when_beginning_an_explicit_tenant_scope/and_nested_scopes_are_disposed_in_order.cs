// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_beginning_an_explicit_tenant_scope;

public class and_nested_scopes_are_disposed_in_order : given.a_tenant_id_accessor
{
    TenantId _outerBefore;
    TenantId _inner;
    TenantId _outerAfter;
    TenantId _after;

    void Establish() => _tenantIdResolver.Resolve().Returns("request-tenant");

    void Because()
    {
        using (_accessor.Begin("acme"))
        {
            _outerBefore = _accessor.Current;
            using (_accessor.Begin("globex"))
            {
                _inner = _accessor.Current;
            }

            _outerAfter = _accessor.Current;
        }

        _after = _accessor.Current;
    }

    [Fact] void should_select_the_outer_tenant() => _outerBefore.ShouldEqual(new TenantId("acme"));
    [Fact] void should_select_the_inner_tenant() => _inner.ShouldEqual(new TenantId("globex"));
    [Fact] void should_restore_the_outer_tenant() => _outerAfter.ShouldEqual(new TenantId("acme"));
    [Fact] void should_restore_request_resolution() => _after.ShouldEqual(new TenantId("request-tenant"));
}
