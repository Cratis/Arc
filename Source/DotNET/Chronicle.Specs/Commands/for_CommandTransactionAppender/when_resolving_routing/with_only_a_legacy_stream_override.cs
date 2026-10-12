// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

public class with_only_a_legacy_stream_override : given.a_definition_routed_command
{
    [Fact]
    void should_validate_against_the_command_source() =>
        Catch.Exception(() => CommandTransactionAppender.ResolveRouting(_invalid with { EventSourceType = EventSourceType.Default }, _context, _eventSources)).ShouldBeOfExactType<EventStreamDoesNotBelongToEventSource>();
    [Fact]
    void should_not_restore_the_definition_for_a_declared_stream() =>
        CommandTransactionAppender.ResolveRouting(_invalid with { EventSourceType = EventSourceType.Default, EventStreamType = "Adjustments" }, _context, _eventSources).EventSource.ShouldBeNull();
}
