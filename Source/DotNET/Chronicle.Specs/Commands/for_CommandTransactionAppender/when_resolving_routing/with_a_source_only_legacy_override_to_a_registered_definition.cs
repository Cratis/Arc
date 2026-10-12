// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

public class with_a_source_only_legacy_override_to_a_registered_definition : given.a_definition_routed_command
{
    EventForEventSourceId _event;
    Exception? _exception;

    void Establish() => _event = new("id", new object()) { EventSourceType = "Ledger" };

    async Task Because() => _exception = await Catch.Exception(async () =>
    {
        var routing = CommandTransactionAppender.ResolveRouting(_event, _context, _eventSources);
        await _eventLog.AppendForCommand(_event.EventSourceId, _event.Event, routing, null);
    });

    [Fact] void should_not_refuse_the_inherited_stream() => _exception.ShouldBeNull();
    [Fact] async Task should_append_by_the_legacy_source_and_inherited_stream_strings() => await _eventLog.Received(1).Append(_event.EventSourceId, _event.Event, eventStreamType: new EventStreamType("Transactions"), eventSourceType: new EventSourceType("Ledger"));
}
