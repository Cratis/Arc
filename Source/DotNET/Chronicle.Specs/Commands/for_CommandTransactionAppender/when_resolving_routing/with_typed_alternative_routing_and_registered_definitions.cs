// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

public class with_typed_alternative_routing_and_registered_definitions : given.a_definition_routed_command
{
    [Fact]
    void should_keep_a_different_declared_definition() =>
        CommandTransactionAppender.ResolveRouting(_valid with { EventSource = typeof(Ledger), EventStream = "Entries" }, _context, _eventSources).EventSource.ShouldEqual(typeof(Ledger));
    [Fact]
    void should_keep_a_different_declared_stream() =>
        CommandTransactionAppender.ResolveRouting(_valid with { EventStream = "Adjustments" }, _context, _eventSources).EventStream.ShouldEqual("Adjustments");
}
