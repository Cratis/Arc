// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

/// <summary>
/// A returned event that names another definition brings its own stream and inherits none of the command's.
/// </summary>
public class with_a_per_event_alternative_event_source : Specification
{
    EventRouting _result;

    void Because() => _result = CommandTransactionAppender.ResolveRouting(new EventForEventSourceId(EventSourceId.New(), new object()) with { EventSource = typeof(LedgerSource) }, ContextWith(true));

    [Fact] void should_use_the_definition_of_the_event() => _result.EventSource.ShouldEqual(typeof(LedgerSource));
    [Fact] void should_not_inherit_the_command_stream() => _result.EventStream.ShouldBeNull();

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
