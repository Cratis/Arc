// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections.for_ResolvedConnection.when_merging;

public class and_the_other_sets_nothing : Specification
{
    ResolvedConnection _first;
    ResolvedConnection _result;

    void Establish() => _first = new("connection", "database", "schema", new("user", "password"));

    void Because() => _result = _first.Merge(ResolvedConnection.Unchanged);

    [Fact] void should_keep_everything() => _result.ShouldEqual(_first);
}
