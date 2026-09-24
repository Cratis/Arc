// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer;

public class when_signaling_condition_pulse : Specification
{
    readonly given.condition_pulse _pulse = new();
    Task _capturedBeforeSignal;
    Task _capturedAfterSignal;

    void Because()
    {
        _capturedBeforeSignal = _pulse.Next;
        _pulse.Signal();
        _capturedAfterSignal = _pulse.Next;
    }

    [Fact] void should_complete_the_waiter_captured_before_the_signal() => _capturedBeforeSignal.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_leave_the_next_waiter_pending() => _capturedAfterSignal.IsCompleted.ShouldBeFalse();
}
