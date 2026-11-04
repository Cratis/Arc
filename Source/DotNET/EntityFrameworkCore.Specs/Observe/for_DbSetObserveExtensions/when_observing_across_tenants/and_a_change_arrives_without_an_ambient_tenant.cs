// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_across_tenants;

public class and_a_change_arrives_without_an_ambient_tenant : a_two_tenant_observe_context
{
    void Because()
    {
        Subscribe(_tenantA);
        InsertWithoutNotification(_tenantA, "a-late");
        using (TenantIdAccessor.Independent.BeginRequest())
        {
            _entityChangeTracker.NotifyChange("TestEntities");
        }

        WaitForEmission(_ => _.Contains("a-late"));
    }

    [Fact] void should_receive_the_new_row() => _emissions.Last().ShouldContain("a-late");
}
