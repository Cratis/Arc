// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

/// <summary>
/// The async-enumerable path pays for a guard twice over — a per-subscription service scope at subscribe time and a
/// dispatch per item — so it needs its own proof that an application without a guard is charged for neither.
/// </summary>
public class and_no_guard_is_registered_for_an_async_enumerable : given.a_guarded_sse_connection
{
    void Establish()
    {
        _streamingData = ABatchEvery10Milliseconds();
        UseGuards(_emissionGuards);
    }

    async Task Because() => await RunConnection(() => WaitFor(() => CountQueryResultsFor(FirstQueryId) > 0));

    [Fact] void should_stream_the_emissions() => CountQueryResultsFor(FirstQueryId).ShouldBeGreaterThan(0);
    [Fact] void should_not_dispatch_to_the_guards() => _emissionGuards.DidNotReceive().Guard(Arg.Any<ObservableQueryEmissionContext>());
}
