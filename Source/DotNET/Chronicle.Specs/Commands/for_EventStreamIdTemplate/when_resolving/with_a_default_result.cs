// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventStreamIdTemplate.when_resolving;

public class with_a_default_result : given.a_stream_id_template
{
    Exception? _error;
    void Because() => _error = Catch.Exception(() => EventStreamIdTemplate.Resolve("{Value}", new { Value = "Default" }));
    [Fact] void should_refuse_the_sentinel() => _error.ShouldBeOfExactType<EventStreamIdTemplatePartMissing>();
}
