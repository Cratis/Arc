// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_CommandEventTags.when_resolving;

public class without_tags : given.a_command_event_tags_resolver
{
    void Because() => _tags = Resolve(new object());

    [Fact] void should_resolve_no_tags() => _tags.ShouldBeEmpty();
}
