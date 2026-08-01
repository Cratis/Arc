// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Sets up a substituted <see cref="IConcurrencyScopeStrategy"/> to behave the way a real one does.
/// </summary>
public static class ConcurrencyScopeStrategyStandIn
{
    /// <summary>
    /// Makes the substitute answer every request with a scope over the dimensions it was asked for, carrying an
    /// actual sequence number.
    /// </summary>
    /// <param name="strategy">The substituted <see cref="IConcurrencyScopeStrategy"/>.</param>
    /// <remarks>
    /// An unconfigured substitute hands back no scope at all, which no real strategy does and which would let a
    /// spec assert on an append that carries no concurrency scope while believing it asserted on one.
    /// </remarks>
    public static void StandInForAnOptimisticStrategy(this IConcurrencyScopeStrategy strategy) =>
        strategy.GetScope(
                Arg.Any<EventSourceId>(),
                Arg.Any<EventStreamType?>(),
                Arg.Any<EventStreamId?>(),
                Arg.Any<EventSourceType?>(),
                Arg.Any<IEnumerable<EventType>?>())
            .Returns(call => Task.FromResult(new ConcurrencyScope(
                EventSequenceNumber.First,
                call.ArgAt<EventSourceId>(0),
                call.ArgAt<EventStreamType?>(1),
                call.ArgAt<EventStreamId?>(2),
                call.ArgAt<EventSourceType?>(3),
                call.ArgAt<IEnumerable<EventType>?>(4))));
}
