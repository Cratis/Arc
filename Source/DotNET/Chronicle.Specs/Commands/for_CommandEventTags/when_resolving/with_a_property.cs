// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.when_resolving;

public class with_a_property : given.a_command_event_tags_resolver
{
    void Because() => _tags = Resolve(new TaggedCommand("project-42"));

    [Fact] void should_read_the_property() => _tags.ShouldEqual([new NamedTag("project", "project-42")]);

    [EventTag("project", nameof(Project))]
    record TaggedCommand(string Project);
}
