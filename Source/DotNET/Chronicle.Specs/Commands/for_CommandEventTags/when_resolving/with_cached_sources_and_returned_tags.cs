// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.when_resolving;

public class with_cached_sources_and_returned_tags : given.a_command_event_tags_resolver
{
    CommandContext _context;
    void Establish()
    {
        _context = ContextFor(new object());
        _applicationProvider.GetEventTags(_context.Command).Returns([new NamedTag("project", "one")]);
    }
    void Because()
    {
        _context.ResolveEventTags();
        new EventTagsCommandResponseValueHandler().UpdateContext(_context, new EventTags(new NamedTag("project", "one"), new NamedTag("project", "two")));
        _tags = _context.ResolveEventTags();
    }

    [Fact] void should_evaluate_providers_only_once() => _applicationProvider.Received(1).GetEventTags(_context.Command);
    [Fact] void should_union_and_deduplicate_returned_tags() => _tags.ShouldEqual([new NamedTag("project", "one"), new NamedTag("project", "two")]);
    [Fact] void should_keep_the_returned_tags_accessor_independent() => _context.GetEventTags().ShouldEqual([new NamedTag("project", "one"), new NamedTag("project", "two")]);
}
