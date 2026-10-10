// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.when_resolving;

public class with_the_command_interface : given.a_command_event_tags_resolver
{
    void Because() => _tags = Resolve(new TaggedCommand());

    [Fact] void should_use_the_supplied_tags() => _tags.ShouldEqual([new NamedTag("project", "computed")]);

    record TaggedCommand : ICanProvideEventTags
    {
        public IEnumerable<NamedTag> GetEventTags() => [new("project", "computed")];
    }
}
