// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_resolver_returns_tenant_id : given.a_tenant_id_accessor
{
    const string ExpectedTenantId = "test-tenant";
    TenantId _result;

    void Establish()
    {
        _tenantIdResolver.Resolve().Returns(ExpectedTenantId);
    }

    void Because() => _result = _accessor.Current;

    [Fact] void should_return_tenant_id_from_resolver() => _result.Value.ShouldEqual(ExpectedTenantId);
    [Fact] void should_call_resolver_once() => _tenantIdResolver.Received(1).Resolve();
}
