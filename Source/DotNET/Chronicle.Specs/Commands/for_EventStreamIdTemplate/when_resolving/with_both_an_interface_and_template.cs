// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventStreamIdTemplate.when_resolving;

public class with_both_an_interface_and_template : given.a_stream_id_template
{
    Exception? _error;
    void Because() => _error = Catch.Exception(() => new EventStreamIdValuesProvider().Provide(new Ambiguous("value")));
    [Fact] void should_preserve_the_ambiguity_error() => _error.ShouldBeOfExactType<AmbiguousEventStreamId>();
}
