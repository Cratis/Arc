// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventStreamIdTemplate.when_resolving;

public class with_a_blank_part : given.a_stream_id_template
{
    Exception? _error;
    void Because() => _error = Catch.Exception(() => EventStreamIdTemplate.ResolveFor(new Composite(new(" "), new(2026, 1, 2))));
    [Fact] void should_refuse_a_blank_property() => _error.ShouldBeOfExactType<EventStreamIdTemplatePartMissing>();
}
