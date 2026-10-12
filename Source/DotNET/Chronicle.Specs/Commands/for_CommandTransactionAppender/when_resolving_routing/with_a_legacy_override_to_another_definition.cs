// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

public class with_a_legacy_override_to_another_definition : given.a_definition_routed_command
{
    [Fact]
    void should_refuse_an_undeclared_stream_of_the_other_definition() =>
        Catch.Exception(() => CommandTransactionAppender.ResolveRouting(_invalid with { EventSourceType = "Ledger" }, _context, _eventSources)).ShouldBeOfExactType<EventStreamDoesNotBelongToEventSource>();
    [Fact]
    void should_keep_a_declared_alternative_as_legacy_routing() =>
        CommandTransactionAppender.ResolveRouting(_invalid with { EventSourceType = "Ledger", EventStreamType = "Entries" }, _context, _eventSources).EventSource.ShouldBeNull();
}
