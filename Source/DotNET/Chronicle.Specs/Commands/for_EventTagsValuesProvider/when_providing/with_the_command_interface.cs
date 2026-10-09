// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;

namespace Cratis.Arc.Chronicle.Commands.for_EventTagsValuesProvider.when_providing;

public class with_the_command_interface : given.an_event_tags_provider
{
    void Because() => _values = _provider.Provide(new TaggedCommand());

    [Fact] void should_use_the_supplied_tags() => ((IEnumerable<NamedTag>)_values[WellKnownCommandContextKeys.EventTags]).ShouldEqual([new NamedTag("project", "computed")]);

    record TaggedCommand : ICanProvideEventTags
    {
        public IEnumerable<NamedTag> GetEventTags() => [new("project", "computed")];
    }
}
