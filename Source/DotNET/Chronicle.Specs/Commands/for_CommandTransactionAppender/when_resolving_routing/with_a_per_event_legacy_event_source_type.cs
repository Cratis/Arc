// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

/// <summary>
/// Routing an event with the legacy strings leaves the command's definition out; appending through it would discard the strings.
/// </summary>
public class with_a_per_event_legacy_event_source_type : Specification
{
    EventRouting _result;

    void Because() => _result = CommandTransactionAppender.ResolveRouting(new EventForEventSourceId(EventSourceId.New(), new object()) with { EventSourceType = new EventSourceType("Ledger") }, ContextWith(true));

    [Fact] void should_not_append_through_the_command_definition() => _result.EventSource.ShouldBeNull();
    [Fact] void should_have_no_stream_name() => _result.EventStream.ShouldBeNull();
    [Fact] void should_use_the_source_type_of_the_event() => _result.EventSourceType.ShouldEqual(new EventSourceType("Ledger"));
    [Fact] void should_use_the_command_stream_type_for_the_value_not_set() => _result.EventStreamType.ShouldEqual(new EventStreamType("Transactions"));

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
