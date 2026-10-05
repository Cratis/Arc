// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

/// <summary>
/// A command that routes with the legacy strings has no definition, and nothing here invents one.
/// </summary>
public class with_a_legacy_command_and_a_plain_event : Specification
{
    EventRouting _result;

    void Because() => _result = CommandTransactionAppender.ResolveRouting(null, ContextWith(false));

    [Fact] void should_have_no_definition() => _result.EventSource.ShouldBeNull();
    [Fact] void should_use_the_command_source_type() => _result.EventSourceType.ShouldEqual(new EventSourceType("Account"));
    [Fact] void should_use_the_command_stream_type() => _result.EventStreamType.ShouldEqual(new EventStreamType("Transactions"));

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
