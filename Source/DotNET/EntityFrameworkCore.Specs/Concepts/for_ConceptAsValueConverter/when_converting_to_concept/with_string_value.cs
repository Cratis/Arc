// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_ConceptAsValueConverter.when_converting_to_concept;

public class with_string_value : given.a_concept_as_value_converter
{
    string _value;
    TestStringConcept _result;

    void Establish()
    {
        _value = "test value";
    }

    void Because() => _result = (TestStringConcept)_stringConverter.ConvertFromProvider(_value);

    [Fact] void should_create_concept_with_the_value() => _result.Value.ShouldEqual("test value");
}
