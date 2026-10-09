// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_EventTagsValuesProvider.when_providing;

public class with_a_property : given.an_event_tags_provider
{
    void Because() => _values = _provider.Provide(new TaggedCommand("project-42"));

    [Fact] void should_read_the_property() => ((IEnumerable<NamedTag>)_values[WellKnownCommandContextKeys.EventTags]).ShouldEqual([new NamedTag("project", "project-42")]);

    [EventTag("project", nameof(Project))]
    record TaggedCommand(string Project);
}
