// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Sinks.for_ReadModelSinks.when_getting_sink_name;

public class for_a_type_a_provider_knows : given.read_model_sinks
{
    string? _result;

    void Establish()
    {
        _first.GetSinkNameFor(typeof(given.ReadModelInDefaultSink)).Returns(" ");
        _second.GetSinkNameFor(typeof(given.ReadModelInDefaultSink)).Returns("documents");
    }

    void Because()
    {
        _sinks.GetSinkNameFor(typeof(given.ReadModelInDefaultSink));
        _result = _sinks.GetSinkNameFor(typeof(given.ReadModelInDefaultSink));
    }

    [Fact] void should_give_the_first_name_that_is_not_blank() => _result.ShouldEqual("documents");
    [Fact] void should_ask_the_providers_once() => _second.Received(1).GetSinkNameFor(typeof(given.ReadModelInDefaultSink));
}
