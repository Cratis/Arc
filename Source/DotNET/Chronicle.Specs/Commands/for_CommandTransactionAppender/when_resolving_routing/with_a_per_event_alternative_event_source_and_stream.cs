// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

/// <summary>
/// The event source and the stream the event names are used together.
/// </summary>
public class with_a_per_event_alternative_event_source_and_stream : Specification
{
    EventRouting _result;

    void Because() => _result = CommandTransactionAppender.ResolveRouting(new EventForEventSourceId(EventSourceId.New(), new object()) with { EventSource = typeof(LedgerSource), EventStream = "Postings" }, ContextWith(true));

    [Fact] void should_use_the_definition_of_the_event() => _result.EventSource.ShouldEqual(typeof(LedgerSource));
    [Fact] void should_use_the_stream_of_the_event() => _result.EventStream.ShouldEqual("Postings");

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
    class LedgerSource;
}
