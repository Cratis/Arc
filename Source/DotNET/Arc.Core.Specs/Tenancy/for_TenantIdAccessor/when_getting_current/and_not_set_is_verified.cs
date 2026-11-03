// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_not_set_is_verified : given.a_verified_tenant_id_accessor
{
    TenantId _result;
    void Establish() => _resolver.Resolve().Returns(string.Empty);
    void Because() => _result = _accessor.Current;
    [Fact] void should_normalize_the_candidate() => _result.ShouldEqual(TenantId.NotSet);
    [Fact] void should_verify_the_default_tenant() => _membership.Received(1).IsMember(_principal, TenantId.NotSet);
    [Fact] void should_not_cache_the_default_tenant() => _accessor.Cached.ShouldBeNull();
}
