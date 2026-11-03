// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.when_handling_connection;

public class and_the_handler_only_reports_legacy_success : for_WebSocketConnectionHandler.given.a_connection_with_scripted_writes
{
    Task Because() => Run(true, true, () => Task.CompletedTask, legacy: true);

    [Fact] void should_not_count_pushes() => _observed.Pushes.ShouldBeEmpty();
    [Fact] void should_keep_the_first_emission_hint() => _firstEmissions.Take(2).ShouldContainOnly(true, true);
}
