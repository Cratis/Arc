// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.when_resolving;

public class with_all_sources : given.a_command_event_tags_resolver
{
    void Establish() => _applicationProvider.GetEventTags(Arg.Any<object>()).Returns([new NamedTag("project", "one"), new NamedTag("tenant", "shared")]);
    void Because() => _tags = Resolve(new TaggedCommand("one"));

    [Fact] void should_union_and_deduplicate_by_name_and_value() => _tags.ShouldEqual([new NamedTag("project", "one"), new NamedTag("origin", "import"), new NamedTag("project", "two"), new NamedTag("tenant", "shared")]);

    [EventTag("project", nameof(Project))]
    [EventTag("origin", Value = "import")]
    record TaggedCommand(string Project) : ICanProvideEventTags
    {
        public IEnumerable<NamedTag> GetEventTags() => [new("project", "one"), new("project", "two")];
    }
}
