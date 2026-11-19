// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_EmissionBacklog;

public class when_entering_beyond_capacity : Specification
{
    EmissionBacklog _backlog;
    DirectObservableEmissionDrain _drain;
    bool[] _accepted;
    int _overflows;
    bool _acceptedAfterLeaving;

    void Establish()
    {
        _backlog = new EmissionBacklog(2);
        _drain = new DirectObservableEmissionDrain(new CancellationTokenSource());
    }

    void Because()
    {
        _accepted = [_backlog.TryEnter(_drain, OnOverflow), _backlog.TryEnter(_drain, OnOverflow), _backlog.TryEnter(_drain, OnOverflow), _backlog.TryEnter(_drain, OnOverflow)];
        _backlog.Leave(_drain);
        _acceptedAfterLeaving = _backlog.TryEnter(_drain, OnOverflow);
    }

    [Fact] void should_accept_up_to_capacity_only() => _accepted.ShouldContainOnly(true, true, false, false);
    [Fact] void should_report_the_overflow_once() => _overflows.ShouldEqual(1);
    [Fact] void should_accept_again_after_leaving() => _acceptedAfterLeaving.ShouldBeTrue();

    void OnOverflow() => _overflows++;
}
