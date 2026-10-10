// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventTagsCommandResponseValueHandler.when_updating_context;

public class with_existing_tags : Specification
{
    CommandContext _context;
    EventTagsCommandResponseValueHandler _handler;

    void Establish()
    {
        _context = new(CorrelationId.New(), typeof(object), new object(), [], new() { { WellKnownCommandContextKeys.EventTags, new NamedTag[] { new("project", "one") } } }, null);
        _handler = new();
    }
    void Because() => _handler.UpdateContext(_context, new EventTags(new NamedTag("project", "one"), new NamedTag("project", "two")));

    [Fact] void should_merge_without_replacing_existing_tags() => _context.GetEventTags().ShouldEqual([new NamedTag("project", "one"), new NamedTag("project", "two")]);
}
