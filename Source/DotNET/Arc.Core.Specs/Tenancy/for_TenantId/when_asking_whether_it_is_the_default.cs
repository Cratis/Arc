// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Tenancy.for_TenantId;

public class when_asking_whether_it_is_the_default : Specification
{
    [Fact] void should_consider_an_unset_tenant_the_default() => TenantId.NotSet.IsDefault.ShouldBeTrue();

    [Fact] void should_consider_the_default_tenant_the_default() => TenantId.Default.IsDefault.ShouldBeTrue();

    [Fact] void should_consider_the_default_tenant_by_value_the_default() => new TenantId("Default").IsDefault.ShouldBeTrue();

    [Fact] void should_not_consider_a_named_tenant_the_default() => new TenantId("tenant-123").IsDefault.ShouldBeFalse();
}
