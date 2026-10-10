// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.when_resolving;

public class with_a_null_property : given.a_command_event_tags_resolver
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => Resolve(new TaggedCommand(null)));

    [Fact] void should_reject_the_missing_value() => _error.ShouldBeOfExactType<EventTagValueMissing>();

    [EventTag("project", nameof(Project))]
    record TaggedCommand(string? Project);
}
