// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventStreamIdTemplate.when_resolving;

public class with_escaped_braces : given.a_stream_id_template
{
    EventStreamId _result;
    void Because() => _result = EventStreamIdTemplate.Resolve("{{{Scope}}}:{Period}", new Composite(new("owner"), new(2026, 1, 2)));

    [Fact] void should_keep_literal_braces() => _result.Value.ShouldEqual("{owner}:2026-01-02");
    [Fact] void should_not_treat_escaped_braces_as_properties() => EventStreamIdTemplate.IsTemplate("{{Scope}}").ShouldBeFalse();
}
