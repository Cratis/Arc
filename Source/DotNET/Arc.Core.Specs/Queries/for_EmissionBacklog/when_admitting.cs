// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_EmissionBacklog;

public class when_admitting : Specification
{
    EmissionBacklog _backlog;
    bool[] _withinCapacity;
    bool _beyondCapacity;
    int _pendingAtOverflow;
    bool _afterRelease;

    void Establish() => _backlog = new EmissionBacklog(3);

    void Because()
    {
        _withinCapacity = [_backlog.TryAdmit(), _backlog.TryAdmit(), _backlog.TryAdmit()];
        _beyondCapacity = _backlog.TryAdmit();
        _pendingAtOverflow = _backlog.Pending;
        _backlog.Release();
        _afterRelease = _backlog.TryAdmit();
    }

    [Fact] void should_admit_up_to_capacity() => _withinCapacity.ShouldContainOnly(true, true, true);
    [Fact] void should_reject_beyond_capacity() => _beyondCapacity.ShouldBeFalse();
    [Fact] void should_not_hold_a_slot_for_a_rejected_emission() => _pendingAtOverflow.ShouldEqual(3);
    [Fact] void should_admit_again_once_a_slot_is_released() => _afterRelease.ShouldBeTrue();
}
