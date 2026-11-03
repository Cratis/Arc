// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.when_handling_connection;

public class and_writes_complete : for_WebSocketConnectionHandler.given.a_connection_with_scripted_writes
{
    Task Because() => Run(true, true, () => Task.CompletedTask);

    [Fact] void should_count_both_pushes() => _observed.Pushes.Count().ShouldEqual(2);
    [Fact] void should_advance_the_first_emission_hint() => _firstEmissions.Take(2).ShouldEqual([true, false]);
}
