// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.when_resolving;

public class with_both_property_and_constant : given.a_command_event_tags_resolver
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => Resolve(new TaggedCommand("one")));

    [Fact] void should_reject_ambiguous_configuration() => _error.ShouldBeOfExactType<AmbiguousEventTagValue>();

    [EventTag("project", nameof(Project), Value = "two")]
    record TaggedCommand(string Project);
}
