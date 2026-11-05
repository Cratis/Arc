// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.given;

namespace Cratis.Arc.Neo4j.for_GraphParameterConverter;

public class when_converting_a_dictionary_and_nothing : Specification
{
    IDictionary<string, object?> _fromDictionary;
    IDictionary<string, object?> _fromNothing;

    void Because()
    {
        _fromDictionary = GraphParameterConverter.Convert(new Dictionary<string, object?> { ["name"] = new PersonName("Ada") });
        _fromNothing = GraphParameterConverter.Convert(null);
    }

    [Fact] void should_unwrap_concepts_in_the_dictionary() => _fromDictionary["name"].ShouldEqual("Ada");
    [Fact] void should_give_no_parameters_for_nothing() => _fromNothing.ShouldBeEmpty();
}
