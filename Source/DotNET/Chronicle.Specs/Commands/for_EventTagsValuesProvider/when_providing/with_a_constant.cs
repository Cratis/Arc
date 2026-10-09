// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_EventTagsValuesProvider.when_providing;

public class with_a_constant : given.an_event_tags_provider
{
    void Because() => _values = _provider.Provide(new TaggedCommand());

    [Fact] void should_use_the_constant() => ((IEnumerable<NamedTag>)_values[WellKnownCommandContextKeys.EventTags]).ShouldEqual([new NamedTag("origin", "import"), new NamedTag("empty", "")]);

    [EventTag("origin", Value = "import")]
    [EventTag("empty", Value = "")]
    record TaggedCommand;
}
