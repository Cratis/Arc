// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ResolvedConnection.when_merging;

public class and_both_expire : Specification
{
    DateTimeOffset _earliest;
    ResolvedConnection _result;

    void Establish() => _earliest = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    void Because() => _result = new ResolvedConnection(ExpiresAt: _earliest.AddHours(1)).Merge(new(ExpiresAt: _earliest));

    [Fact] void should_expire_at_the_earliest() => _result.ExpiresAt.ShouldEqual(_earliest);
}
