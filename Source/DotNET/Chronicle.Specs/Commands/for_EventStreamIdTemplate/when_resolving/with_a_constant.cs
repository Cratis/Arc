// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventStreamIdTemplate.when_resolving;

public class with_a_constant : given.a_stream_id_template
{
    string? _result;
    void Because() => _result = EventStreamIdTemplate.ResolveFor(new Constant())?.Value;
    [Fact] void should_leave_the_constant_unchanged() => _result.ShouldEqual("constant");
}
