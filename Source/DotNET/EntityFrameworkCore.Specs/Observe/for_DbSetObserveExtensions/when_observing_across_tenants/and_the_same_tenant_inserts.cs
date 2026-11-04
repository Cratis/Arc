// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_across_tenants;

public class and_the_same_tenant_inserts : a_two_tenant_observe_context
{
    void Because()
    {
        Subscribe(_tenantA);
        Insert(_tenantA, "a-new");
        WaitForEmission(_ => _.Contains("a-new"));
    }

    [Fact] void should_receive_the_new_row() => _emissions.Last().ShouldContain("a-new");
}
