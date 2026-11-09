// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Sinks.for_ReadModelSinks.when_getting_sink_name;

public class for_a_type_declaring_a_sink : given.read_model_sinks
{
    string? _result;

    void Establish() => _first.GetSinkNameFor(typeof(given.ReadModelInNamedSink)).Returns("other");

    void Because() => _result = _sinks.GetSinkNameFor(typeof(given.ReadModelInNamedSink));

    [Fact] void should_give_the_declared_name_trimmed() => _result.ShouldEqual("reporting");
    [Fact] void should_not_ask_the_providers() => _first.DidNotReceive().GetSinkNameFor(Arg.Any<Type>());
}
