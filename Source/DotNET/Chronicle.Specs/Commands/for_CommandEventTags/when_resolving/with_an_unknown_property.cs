// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.when_resolving;

public class with_an_unknown_property : given.a_command_event_tags_resolver
{
    Exception _error;
    void Because() => _error = Catch.Exception(() => Resolve(new TaggedCommand()));

    [Fact] void should_report_the_unknown_property() => _error.ShouldBeOfExactType<UnknownEventTagProperty>();

    [EventTag("project", "Missing")]
    record TaggedCommand;
}
