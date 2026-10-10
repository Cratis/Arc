// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventStreamIdTemplate.when_resolving;

public class with_an_unknown_property : given.a_stream_id_template
{
    Exception? _error;
    void Because() => _error = Catch.Exception(() => EventStreamIdTemplate.Resolve("{Missing}", new Constant()));
    [Fact] void should_reject_the_template() => _error.ShouldBeOfExactType<UnknownEventStreamIdTemplateProperty>();
}
