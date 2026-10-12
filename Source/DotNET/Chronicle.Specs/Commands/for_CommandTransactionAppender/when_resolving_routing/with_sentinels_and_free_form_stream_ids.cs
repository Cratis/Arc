// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CommandTransactionAppender.when_resolving_routing;

public class with_sentinels_and_free_form_stream_ids : given.a_definition_routed_command
{
    [Fact]
    void should_keep_the_command_definition_for_unset_legacy_routes() =>
        CommandTransactionAppender.ResolveRouting(_invalid with { EventSourceType = EventSourceType.Default, EventStreamType = EventStreamType.All, EventStreamId = "free-form" }, _context, _eventSources).EventSource.ShouldEqual(typeof(Account));

    [Fact]
    void should_not_validate_an_unset_resolved_stream()
    {
        _context.Values[WellKnownCommandContextKeys.EventStreamType] = EventStreamType.All;
        CommandTransactionAppender.ResolveRouting(_invalid with { EventStreamType = EventStreamType.All }, _context, _eventSources).EventStreamType.ShouldEqual(EventStreamType.All);
    }
}
