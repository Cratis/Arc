// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventTagsValuesProvider.when_providing;

public class with_a_null_property : given.an_event_tags_provider
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => _provider.Provide(new TaggedCommand(null)));

    [Fact] void should_reject_the_missing_value() => _error.ShouldBeOfExactType<EventTagValueMissing>();

    [EventTag("project", nameof(Project))]
    record TaggedCommand(string? Project);
}
