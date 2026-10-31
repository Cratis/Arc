// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantIdAccessor.when_getting_current;

public class and_membership_is_verified : given.a_verified_tenant_id_accessor
{
    TenantId _result;
    void Because() => _result = _accessor.Current;
    [Fact] void should_return_the_selected_tenant() => _result.ShouldEqual(new TenantId("selected"));
    [Fact] void should_use_the_request_principal() => _membership.Received(1).IsMember(_principal, _result);
    [Fact] void should_not_cache_a_verified_result() => _accessor.Cached.ShouldBeNull();
}
