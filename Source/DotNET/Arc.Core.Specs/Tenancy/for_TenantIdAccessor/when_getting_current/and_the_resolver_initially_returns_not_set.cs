// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_the_resolver_initially_returns_not_set : given.a_tenant_id_accessor
{
    TenantId _firstResult;
    TenantId _secondResult;
    TenantId _thirdResult;

    void Establish()
    {
        _tenantIdResolver.Resolve().Returns(TenantId.NotSet.Value, "resolved");
        _firstResult = _accessor.Current;
    }

    void Because()
    {
        _secondResult = _accessor.Current;
        _thirdResult = _accessor.Current;
    }

    [Fact] void should_return_not_set_initially() => _firstResult.ShouldEqual(TenantId.NotSet);
    [Fact] void should_retry_resolving_the_tenant() => _secondResult.ShouldEqual(new TenantId("resolved"));
    [Fact] void should_cache_the_resolved_tenant() => _thirdResult.ShouldEqual(_secondResult);
    [Fact] void should_call_the_resolver_only_until_it_resolves() => _tenantIdResolver.Received(2).Resolve();
}
