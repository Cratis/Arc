// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Tenancy.for_TenantMembershipDenied;

public class when_checking_the_cause : Specification
{
    bool _publicForSnapshot;
    bool _unauthorizedForSnapshot;
    bool _publicForDenial;

    void Because()
    {
        var snapshot = new InvalidOperationException("wrapped", new OperationSnapshotUnavailable());
        _publicForSnapshot = TenantMembershipDenied.IsCauseOf(snapshot);
        _unauthorizedForSnapshot = UnauthorizedCause.IsCauseOf(snapshot);
        _publicForDenial = TenantMembershipDenied.IsCauseOf(new TenantMembershipDenied());
    }

    [Fact] void should_not_classify_an_unavailable_snapshot_as_a_membership_denial() => _publicForSnapshot.ShouldBeFalse();
    [Fact] void should_reject_an_unavailable_snapshot_as_unauthorized() => _unauthorizedForSnapshot.ShouldBeTrue();
    [Fact] void should_still_classify_a_membership_denial() => _publicForDenial.ShouldBeTrue();
}
