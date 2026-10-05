// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

/// <summary>
/// A plain event follows the definition and stream the command declares.
/// </summary>
public class with_a_command_event_source_and_a_plain_event : Specification
{
    EventRouting _result;

    void Because() => _result = CommandTransactionAppender.ResolveRouting(null, ContextWith(true));

    [Fact] void should_use_the_command_definition() => _result.EventSource.ShouldEqual(typeof(AccountSource));
    [Fact] void should_use_the_command_stream() => _result.EventStream.ShouldEqual("Transactions");

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
