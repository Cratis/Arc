// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_called_multiple_times : given.a_tenant_id_accessor
{
    const string ExpectedTenantId = "test-tenant";
    TenantId _firstResult;
    TenantId _secondResult;

    void Establish()
    {
        _tenantIdResolver.Resolve().Returns(ExpectedTenantId);
    }

    void Because()
    {
        _firstResult = _accessor.Current;
        _secondResult = _accessor.Current;
    }

    [Fact] void should_return_same_tenant_id_both_times() => _secondResult.ShouldEqual(_firstResult);
    [Fact] void should_call_resolver_only_once() => _tenantIdResolver.Received(1).Resolve();
}
