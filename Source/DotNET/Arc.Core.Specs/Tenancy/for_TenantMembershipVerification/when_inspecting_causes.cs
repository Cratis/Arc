// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Tenancy.for_TenantMembershipVerification;

public class when_inspecting_causes : Specification
{
    bool _direct;
    bool _nested;
    bool _ordinary;
    bool _tooDeep;
    void Because()
    {
        _direct = TenantMembershipDenied.IsCauseOf(new TenantMembershipDenied());
        _nested = TenantMembershipDenied.IsCauseOf(new TargetInvocationException(new AggregateException(new InvalidOperationException(), new TenantMembershipDenied())));
        _ordinary = TenantMembershipDenied.IsCauseOf(new InvalidOperationException());
        Exception deep = new TenantMembershipDenied();
        for (var level = 0; level < 20; level++)
        {
            deep = new TargetInvocationException(deep);
        }
        _tooDeep = TenantMembershipDenied.IsCauseOf(deep);
    }
    [Fact] void should_find_a_direct_denial() => _direct.ShouldBeTrue();
    [Fact] void should_search_all_aggregate_causes() => _nested.ShouldBeTrue();
    [Fact] void should_preserve_unrelated_errors() => _ordinary.ShouldBeFalse();
    [Fact] void should_bound_the_search() => _tooDeep.ShouldBeFalse();
}
