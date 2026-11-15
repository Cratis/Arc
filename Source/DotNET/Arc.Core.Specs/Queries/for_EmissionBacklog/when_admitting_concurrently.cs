// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_EmissionBacklog;

public class when_admitting_concurrently : Specification
{
    const int Capacity = 10;

    EmissionBacklog _backlog;
    int _admitted;

    void Establish() => _backlog = new EmissionBacklog(Capacity);

    void Because() =>
        Parallel.For(0, 1000, _ =>
        {
            if (_backlog.TryAdmit())
            {
                Interlocked.Increment(ref _admitted);
            }
        });

    [Fact] void should_admit_exactly_the_capacity() => _admitted.ShouldEqual(Capacity);
    [Fact] void should_hold_exactly_the_capacity() => _backlog.Pending.ShouldEqual(Capacity);
}
