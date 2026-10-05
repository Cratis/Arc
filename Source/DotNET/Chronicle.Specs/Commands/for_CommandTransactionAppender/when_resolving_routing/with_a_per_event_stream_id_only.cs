// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

/// <summary>
/// A stream id on the event is orthogonal to the definition, which stays in force.
/// </summary>
public class with_a_per_event_stream_id_only : Specification
{
    EventRouting _result;

    void Because() => _result = CommandTransactionAppender.ResolveRouting(new EventForEventSourceId(EventSourceId.New(), new object()) with { EventStreamId = new EventStreamId("2026-10") }, ContextWith(true));

    [Fact] void should_keep_the_command_definition() => _result.EventSource.ShouldEqual(typeof(AccountSource));
    [Fact] void should_use_the_stream_id_of_the_event() => _result.EventStreamId.ShouldEqual(new EventStreamId("2026-10"));

    static CommandContext ContextWith(bool definition)
    {
        var values = new CommandContextValues
        {
            { WellKnownCommandContextKeys.EventSourceType, new EventSourceType("Account") },
            { WellKnownCommandContextKeys.EventStreamType, new EventStreamType("Transactions") }
        };
        if (definition)
        {
            values[WellKnownCommandContextKeys.EventSource] = typeof(AccountSource);
            values[WellKnownCommandContextKeys.EventStream] = "Transactions";
        }

        return new(CorrelationId.New(), typeof(object), new object(), [], values, null);
    }

    class AccountSource;
}
