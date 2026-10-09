// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.when_resolving;

public class without_a_service_provider : Specification
{
    CommandContext _context;
    IEnumerable<NamedTag> _tags;
    void Establish()
    {
        var command = new TaggedCommand();
        _context = new(CorrelationId.New(), typeof(TaggedCommand), command, [], []);
        new EventTagsCommandResponseValueHandler().UpdateContext(_context, new EventTags(new NamedTag("returned", "value")));
    }
    void Because() => _tags = _context.ResolveEventTags();

    [Fact] void should_resolve_command_and_returned_tags() => _tags.ShouldEqual([new NamedTag("origin", "import"), new NamedTag("project", "one"), new NamedTag("returned", "value")]);
    [Fact] void should_expose_only_returned_tags_without_resolving() => _context.GetEventTags().ShouldEqual([new NamedTag("returned", "value")]);

    [EventTag("origin", Value = "import")]
    record TaggedCommand : ICanProvideEventTags
    {
        public IEnumerable<NamedTag> GetEventTags() => [new("project", "one")];
    }
}
