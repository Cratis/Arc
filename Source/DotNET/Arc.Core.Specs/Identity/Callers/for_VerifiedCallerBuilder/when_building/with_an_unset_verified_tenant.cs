// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers.for_VerifiedCallerBuilder.when_building;

public class with_an_unset_verified_tenant : given.a_builder
{
    void Establish()
    {
        _tenant = TenantId.NotSet;
        _facts = _facts with { Tenant = TenantId.NotSet };
    }
    void Because() => Build();

    [Fact] void should_refuse_the_verified_sentinel() => _result.Refusal.ShouldEqual(CallerRefusal.TenantMismatch);
    [Fact] void should_not_build_a_caller() => _result.Caller.ShouldBeNull();
}
