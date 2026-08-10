// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ClientEnumerableObservable.when_handling_connection;

public class and_guard_denies_an_emission : given.a_guarded_client_enumerable_observable
{
    void Establish() => _verdict = _ => ObservableQueryEmissionVerdict.DenyAndTerminate;

    async Task Because() => await RunConnection();

    [Fact] void should_not_write_the_emission() => _sent.Count(_ => _.IsAuthorized).ShouldEqual(0);
    [Fact] void should_write_a_terminal_unauthorized_result() => _sent.Count(_ => !_.IsAuthorized).ShouldEqual(1);
    [Fact] void should_stop_consulting_the_guard() => _guardCalls.Count.ShouldEqual(1);
}
