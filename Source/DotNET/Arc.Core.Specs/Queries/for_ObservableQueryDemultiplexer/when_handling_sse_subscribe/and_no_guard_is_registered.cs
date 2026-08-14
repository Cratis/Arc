// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

/// <summary>
/// The promise this whole seam is built on is that an application that never implements a guard pays nothing for it.
/// Nothing else pins that: every unguarded spec merely relies on a substitute's default and asserts what was written,
/// so hoisting the guard consultation out from behind the <see cref="IObservableQueryEmissionGuards.HasGuards"/> gate
/// would stay green while costing every existing Arc consumer a dispatch on every emission of every subscription.
/// </summary>
public class and_no_guard_is_registered : given.a_guarded_sse_connection
{
    void Establish() => UseGuards(_emissionGuards);

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["event-store-a"]);
        await WaitFor(() => CountQueryResultsFor(FirstQueryId) == 1);
    });

    [Fact] void should_write_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
    [Fact] void should_not_dispatch_to_the_guards() => _emissionGuards.DidNotReceive().Guard(Arg.Any<ObservableQueryEmissionContext>());
}
