// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class and_later_writes_are_not_confirmed : for_WebSocketConnectionHandler.given.a_connection_with_scripted_writes
{
    Task Because() => Run(false, true, () => Task.FromException(new OperationCanceledException()), initiallySent: true);

    [Fact] void should_only_count_the_confirmed_push() => _observed.Pushes.Count().ShouldEqual(1);
    [Fact] void should_not_reset_the_first_emission_hint() => _firstEmissions.Take(3).ShouldEqual([true, false, false]);
}
