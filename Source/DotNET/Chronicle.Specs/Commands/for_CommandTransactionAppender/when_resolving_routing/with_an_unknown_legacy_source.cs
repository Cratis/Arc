// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

public class with_an_unknown_legacy_source : given.a_definition_routed_command
{
    [Fact] void should_preserve_the_legacy_route() => CommandTransactionAppender.ResolveRouting(_invalid with { EventSourceType = "Unknown" }, _context, _eventSources).EventSourceType!.Value.ShouldEqual("Unknown");
}
