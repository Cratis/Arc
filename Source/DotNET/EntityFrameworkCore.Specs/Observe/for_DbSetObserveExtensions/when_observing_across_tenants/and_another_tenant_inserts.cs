// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_across_tenants;

public class and_another_tenant_inserts : a_two_tenant_observe_context
{
    TenantId _tenantDuringWrite;

    void Because()
    {
        Subscribe(_tenantA);
        using (_tenantScope.Begin(_tenantB))
        {
            Insert(_tenantB, "b-new");
            _tenantDuringWrite = _tenantIdAccessor.Current;
        }

        WaitForEmission(_ => _.Contains("b-new"));
    }

    [Fact] void should_not_emit_rows_of_the_other_tenant() => _emissions.SelectMany(_ => _).ShouldNotContain(name => name.StartsWith("b-", StringComparison.Ordinal));
    [Fact] void should_still_contain_its_own_rows() => _emissions.Last().ShouldContain("a-row");
    [Fact] void should_keep_the_writers_tenant_after_saving() => _tenantDuringWrite.ShouldEqual(_tenantB);
}
