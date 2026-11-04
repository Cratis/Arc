// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientObservable.when_handling_connection;

public class and_the_socket_is_closed_before_writing : for_WebSocketConnectionHandler.given.a_connection_with_scripted_writes
{
    Task Because() => Run(false, false, () => Task.CompletedTask);

    [Fact] void should_not_count_pushes() => _observed.Pushes.ShouldBeEmpty();
    [Fact] void should_keep_the_first_emission_hint() => _firstEmissions.Take(2).ShouldContainOnly(true, true);
}
