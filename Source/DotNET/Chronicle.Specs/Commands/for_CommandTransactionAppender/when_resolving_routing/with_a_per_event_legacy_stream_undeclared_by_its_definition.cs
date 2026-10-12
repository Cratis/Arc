// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

public class with_a_per_event_legacy_stream_undeclared_by_its_definition : given.a_definition_routed_command
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => CommandTransactionAppender.ResolveRouting(_invalid, _context, _eventSources));

    [Fact] void should_refuse_the_route() => _error.ShouldBeOfExactType<EventStreamDoesNotBelongToEventSource>();
    [Fact] void should_name_the_source_and_stream() => _error.Message.ShouldContain("Account");
    [Fact] void should_name_the_undeclared_stream() => _error.Message.ShouldContain("Typo");
}
