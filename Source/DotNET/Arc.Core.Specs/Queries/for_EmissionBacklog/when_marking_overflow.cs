// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_EmissionBacklog;

public class when_marking_overflow : Specification
{
    EmissionBacklog _backlog;
    bool _first;
    bool _second;

    void Establish() => _backlog = new EmissionBacklog(1);

    void Because()
    {
        _first = _backlog.TryMarkOverflowed();
        _second = _backlog.TryMarkOverflowed();
    }

    [Fact] void should_report_the_first_mark() => _first.ShouldBeTrue();
    [Fact] void should_not_report_a_later_mark() => _second.ShouldBeFalse();
}
