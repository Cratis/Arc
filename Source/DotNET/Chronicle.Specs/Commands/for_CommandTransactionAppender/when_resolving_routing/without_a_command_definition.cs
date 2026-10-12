// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

public class without_a_command_definition : given.a_definition_routed_command
{
    void Establish() => _context.Values.Remove(WellKnownCommandContextKeys.EventSource);

    [Fact] void should_not_refuse_legacy_routing() => CommandTransactionAppender.ResolveRouting(_invalid, _context, _eventSources).EventStreamType!.Value.ShouldEqual("Typo");
}
