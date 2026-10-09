// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Commands.for_EventTagsValuesProvider.when_providing;

public class with_a_concept : given.an_event_tags_provider
{
    void Because() => _values = _provider.Provide(new TaggedCommand(new Project("project-42")));

    [Fact] void should_read_the_underlying_value() => ((IEnumerable<NamedTag>)_values[WellKnownCommandContextKeys.EventTags]).ShouldEqual([new NamedTag("project", "project-42")]);

    record Project(string Value) : ConceptAs<string>(Value);
    [EventTag("project", nameof(Project))]
    record TaggedCommand(Project Project);
}
