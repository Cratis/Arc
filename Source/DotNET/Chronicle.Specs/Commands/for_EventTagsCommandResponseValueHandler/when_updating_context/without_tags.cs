// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventTagsCommandResponseValueHandler.when_updating_context;

public class without_tags : Specification
{
    CommandContext _context;
    void Establish() => _context = new(CorrelationId.New(), typeof(object), new object(), [], [], null);
    void Because() => new EventTagsCommandResponseValueHandler().UpdateContext(_context, new EventTags());

    [Fact] void should_not_write_the_key() => _context.Values.ContainsKey(WellKnownCommandContextKeys.EventTags).ShouldBeFalse();
    [Fact] void should_resolve_to_empty_tags() => _context.GetEventTags().ShouldBeEmpty();
}
