// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_resolver_returns_null : given.a_tenant_id_accessor
{
    TenantId _result;

    void Establish()
    {
        _tenantIdResolver.Resolve().Returns((string)null!);
    }

    void Because() => _result = _accessor.Current;

    [Fact] void should_return_not_set_tenant_id() => _result.ShouldEqual(TenantId.NotSet);
}
